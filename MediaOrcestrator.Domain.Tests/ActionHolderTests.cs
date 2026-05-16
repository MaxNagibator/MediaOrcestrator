using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
public class ActionHolderTests
{
    private static ActionHolder CreateHolder()
    {
        return new(NullLogger<ActionHolder>.Instance);
    }

    private static bool ContainsAction(ActionHolder holder, Guid id)
    {
        return holder.Snapshot().Any(a => a.Id == id);
    }

    [Test]
    public void Регистрация_создаёт_активное_действие_в_реестре()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();

        var act = holder.Register("test", "Старт", 10, cts);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(act.Name, Is.EqualTo("test"));
            Assert.That(act.Status, Is.EqualTo("Старт"));
            Assert.That(act.ProgressMax, Is.EqualTo(10));
            Assert.That(act.ProgressValue, Is.Zero);
            Assert.That(ContainsAction(holder, act.Id), Is.True);
        }
    }

    [Test]
    public void Завершение_не_отменяет_токен()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Finish();

        Assert.That(act.CancellationTokenSource.IsCancellationRequested, Is.False);
    }

    [Test]
    public void Завершение_удаляет_действие_из_реестра()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Finish();

        Assert.That(ContainsAction(holder, act.Id), Is.False);
    }

    [Test]
    public void Завершение_проставляет_статус_по_умолчанию_и_пользовательский()
    {
        var holder = CreateHolder();
        var cts1 = new CancellationTokenSource();
        var actDefault = holder.Register("a", "Старт", 0, cts1);

        actDefault.Finish();

        Assert.That(actDefault.Status, Is.EqualTo("Выполнено"));

        var cts2 = new CancellationTokenSource();
        var actCustom = holder.Register("b", "Старт", 0, cts2);

        actCustom.Finish("Готово");

        Assert.That(actCustom.Status, Is.EqualTo("Готово"));
    }

    [Test]
    public void Отмена_отменяет_токен_и_удаляет_действие()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cts.IsCancellationRequested, Is.True);
            Assert.That(act.Status, Is.EqualTo("Отменено"));
            Assert.That(ContainsAction(holder, act.Id), Is.False);
        }
    }

    [Test]
    public void Повторное_завершение_не_перезаписывает_статус()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Finish("X");
        act.Finish("Y");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.Status, Is.EqualTo("X"));
            Assert.That(ContainsAction(holder, act.Id), Is.False);
        }
    }

    [Test]
    public void Отмена_после_завершения_не_дёргает_токен()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Finish("Готово");
        act.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cts.IsCancellationRequested, Is.False);
            Assert.That(act.Status, Is.EqualTo("Готово"));
        }
    }

    [Test]
    public void Завершение_после_отмены_сохраняет_статус_отмены()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Cancel();
        act.Finish();

        Assert.That(act.Status, Is.EqualTo("Отменено"));
    }

    [Test]
    public async Task Инкремент_прогресса_потокобезопасен_под_параллельной_нагрузкой()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 100_000, cts);

        const int Tasks = 1000;
        const int Increments = 100;

        var pending = new Task[Tasks];
        for (var i = 0; i < Tasks; i++)
        {
            pending[i] = Task.Run(() =>
            {
                for (var j = 0; j < Increments; j++)
                {
                    act.ProgressPlus();
                }
            });
        }

        await Task.WhenAll(pending);

        Assert.That(act.ProgressValue, Is.EqualTo(Tasks * Increments));
    }

    [Test]
    public void Инкремент_через_холдер_увеличивает_прогресс()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 10, cts);

        holder.ProgressPlus(act.Id);
        holder.ProgressPlus(act.Id);
        holder.ProgressPlus(act.Id);

        Assert.That(act.ProgressValue, Is.EqualTo(3));
    }

    [Test]
    public void Установка_статуса_через_холдер_эквивалентна_установке_через_свойство()
    {
        var holder = CreateHolder();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();

        var actA = holder.Register("a", "Старт", 0, cts1);
        var actB = holder.Register("b", "Старт", 0, cts2);

        actA.Status = "Новый";
        holder.SetStatus(actB.Id, "Новый");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actB.Status, Is.EqualTo(actA.Status));
            Assert.That(actA.Status, Is.EqualTo("Новый"));
        }
    }

    [Test]
    public void Регистрация_порождает_событие_изменения_реестра()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();
        var handler = Substitute.For<EventHandler>();
        holder.Changed += handler;

        holder.Register("test", "Старт", 0, cts);

        handler.Received(1).Invoke(holder, Arg.Any<EventArgs>());
    }

    [Test]
    public void Отмена_порождает_событие_изменения_реестра()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);
        var handler = Substitute.For<EventHandler>();
        holder.Changed += handler;

        act.Cancel();

        handler.Received(1).Invoke(holder, Arg.Any<EventArgs>());
    }

    [Test]
    public void Завершение_порождает_событие_изменения_реестра()
    {
        var holder = CreateHolder();
        var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);
        var handler = Substitute.For<EventHandler>();
        holder.Changed += handler;

        act.Finish();

        handler.Received(1).Invoke(holder, Arg.Any<EventArgs>());
    }

    [Test]
    public void Изменение_статуса_порождает_событие_действия()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);
        var handler = Substitute.For<EventHandler>();
        act.Changed += handler;

        act.Status = "X";

        handler.Received(1).Invoke(act, Arg.Any<EventArgs>());
    }

    [Test]
    public void Инкремент_прогресса_порождает_событие_действия()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 10, cts);
        var handler = Substitute.For<EventHandler>();
        act.Changed += handler;

        act.ProgressPlus();

        handler.Received(1).Invoke(act, Arg.Any<EventArgs>());
    }

    [Test]
    public void Снимок_возвращает_текущие_действия()
    {
        var holder = CreateHolder();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        using var cts3 = new CancellationTokenSource();

        holder.Register("a", "Старт", 0, cts1);
        holder.Register("b", "Старт", 0, cts2);
        holder.Register("c", "Старт", 0, cts3);

        var names = holder.Snapshot().Select(a => a.Name).ToArray();

        Assert.That(names, Is.EquivalentTo(["a", "b", "c"]));
    }

    [Test]
    public void Снимок_не_содержит_завершённое_действие()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();
        var act = holder.Register("test", "Старт", 0, cts);

        act.Finish();

        Assert.That(holder.Snapshot(), Is.Empty);
    }

    [Test]
    public async Task Область_переносится_через_TaskRun_и_вложенные_области()
    {
        var holder = CreateHolder();
        using var chainCts = new CancellationTokenSource();
        using var transferCts = new CancellationTokenSource();
        using var downloadCts = new CancellationTokenSource();

        var chain = holder.Register("chain", "Старт", 0, chainCts);

        ActionHolder.RunningAction transfer = null!;
        ActionHolder.RunningAction download = null!;

        using (holder.BeginScope(chain))
        {
            await Task.Run(async () =>
            {
                transfer = holder.Register("transfer", "Старт", 0, transferCts);
                using (holder.BeginScope(transfer))
                {
                    await Task.Yield();
                    download = holder.Register("download", "Старт", 0, downloadCts);
                }
            });
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(transfer.ParentId, Is.EqualTo(chain.Id));
            Assert.That(transfer.Depth, Is.EqualTo(1));
            Assert.That(download.ParentId, Is.EqualTo(transfer.Id));
            Assert.That(download.Depth, Is.EqualTo(2));
        }
    }

    [Test]
    public void Действие_без_области_остаётся_корневым()
    {
        var holder = CreateHolder();
        using var cts = new CancellationTokenSource();

        var act = holder.Register("root", "Старт", 0, cts);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.ParentId, Is.Null);
            Assert.That(act.Depth, Is.Zero);
        }
    }

    [Test]
    public void Регистрация_в_области_делает_действие_потомком()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        using var childCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);

        ActionHolder.RunningAction child;
        using (holder.BeginScope(parent))
        {
            child = holder.Register("child", "Старт", 0, childCts);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(child.ParentId, Is.EqualTo(parent.Id));
            Assert.That(child.Depth, Is.EqualTo(1));
            Assert.That(parent.Depth, Is.Zero);
        }
    }

    [Test]
    public void Вложенные_области_восстанавливают_прежнего_родителя()
    {
        var holder = CreateHolder();
        using var aCts = new CancellationTokenSource();
        using var bCts = new CancellationTokenSource();
        using var cCts = new CancellationTokenSource();
        using var dCts = new CancellationTokenSource();

        var a = holder.Register("a", "Старт", 0, aCts);
        var b = holder.Register("b", "Старт", 0, bCts);

        ActionHolder.RunningAction c;
        ActionHolder.RunningAction d;
        using (holder.BeginScope(a))
        {
            using (holder.BeginScope(b))
            {
                c = holder.Register("c", "Старт", 0, cCts);
            }

            d = holder.Register("d", "Старт", 0, dCts);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(c.ParentId, Is.EqualTo(b.Id));
            Assert.That(c.Depth, Is.EqualTo(1));
            Assert.That(d.ParentId, Is.EqualTo(a.Id));
            Assert.That(d.Depth, Is.EqualTo(1));
        }
    }

    [Test]
    public void Снимок_ставит_потомка_сразу_после_родителя()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        using var childCts = new CancellationTokenSource();
        using var otherCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);
        using (holder.BeginScope(parent))
        {
            holder.Register("child", "Старт", 0, childCts);
        }

        holder.Register("other", "Старт", 0, otherCts);

        var names = holder.Snapshot().Select(a => a.Name).ToArray();

        Assert.That(names, Is.EqualTo(["parent", "child", "other"]));
    }

    [Test]
    public void Отмена_родителя_каскадно_отменяет_потомков()
    {
        var holder = CreateHolder();
        var parentCts = new CancellationTokenSource();
        var childCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);
        ActionHolder.RunningAction child;
        using (holder.BeginScope(parent))
        {
            child = holder.Register("child", "Старт", 0, childCts);
        }

        parent.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ContainsAction(holder, parent.Id), Is.False);
            Assert.That(ContainsAction(holder, child.Id), Is.False);
            Assert.That(child.Status, Is.EqualTo("Отменено"));
            Assert.That(childCts.IsCancellationRequested, Is.True);
        }
    }

    [Test]
    public void Потомок_завершённого_родителя_остаётся_в_снимке()
    {
        var holder = CreateHolder();
        var parentCts = new CancellationTokenSource();
        using var childCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);
        ActionHolder.RunningAction child;
        using (holder.BeginScope(parent))
        {
            child = holder.Register("child", "Старт", 0, childCts);
        }

        parent.Finish();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ContainsAction(holder, parent.Id), Is.False);
            Assert.That(ContainsAction(holder, child.Id), Is.True);
        }
    }
}
