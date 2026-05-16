using Microsoft.Extensions.Logging.Abstractions;

namespace MediaOrcestrator.Domain.Tests;

[TestFixture]
public class ActionHolderExplicitParentTests
{
    private static ActionHolder CreateHolder()
    {
        return new(NullLogger<ActionHolder>.Instance);
    }

    private static bool ContainsAction(ActionHolder holder, Guid id)
    {
        return holder.Snapshot().Any(a => a.Id == id);
    }

    private static bool IsCompleted(ActionHolder holder, Guid id)
    {
        return holder.CompletedSnapshot().Any(a => a.Id == id);
    }

    [Test]
    public void Явно_переданный_родитель_выставляет_ParentId_и_Depth()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        using var childCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);
        var child = holder.Register("child", "Старт", 0, childCts, parent: parent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(child.ParentId, Is.EqualTo(parent.Id));
            Assert.That(child.Depth, Is.EqualTo(1));
            Assert.That(parent.Depth, Is.Zero);
        }
    }

    [Test]
    public void Явный_родитель_важнее_окружающей_области()
    {
        var holder = CreateHolder();
        using var ambientCts = new CancellationTokenSource();
        using var explicitCts = new CancellationTokenSource();
        using var childCts = new CancellationTokenSource();

        var ambient = holder.Register("ambient", "Старт", 0, ambientCts);
        var explicitParent = holder.Register("explicit", "Старт", 0, explicitCts);

        ActionHolder.RunningAction child;
        using (holder.BeginScope(ambient))
        {
            child = holder.Register("child", "Старт", 0, childCts, parent: explicitParent);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(child.ParentId, Is.EqualTo(explicitParent.Id));
            Assert.That(child.Depth, Is.EqualTo(1));
        }
    }

    [Test]
    public void Дети_с_явным_родителем_видны_как_потомки_в_снимке()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        using var firstCts = new CancellationTokenSource();
        using var secondCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);
        holder.Register("first", "Старт", 0, firstCts, parent: parent);
        holder.Register("second", "Старт", 0, secondCts, parent: parent);

        var names = holder.Snapshot().Select(a => a.Name).ToArray();

        Assert.That(names, Is.EqualTo(["parent", "first", "second"]));
    }

    [Test]
    public void Прогресс_родителя_растёт_по_мере_завершения_детей()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        using var firstCts = new CancellationTokenSource();
        using var secondCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "0 / 2 источников", 2, parentCts);
        var first = holder.Register("first", "Старт", 0, firstCts, parent: parent);
        var second = holder.Register("second", "Старт", 0, secondCts, parent: parent);

        Assert.That(parent.ProgressValue, Is.Zero);

        first.Finish();
        parent.ProgressPlus();

        Assert.That(parent.ProgressValue, Is.EqualTo(1));

        second.Finish();
        parent.ProgressPlus();

        Assert.That(parent.ProgressValue, Is.EqualTo(2));
    }

    [Test]
    public void Родитель_не_остаётся_активным_когда_завершён_в_finally_после_успеха()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        var parent = holder.Register("parent", "Старт", 1, parentCts);

        try
        {
        }
        finally
        {
            parent.Finish("Готово: 1 из 1 источников");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ContainsAction(holder, parent.Id), Is.False);
            Assert.That(IsCompleted(holder, parent.Id), Is.True);
            Assert.That(parent.State, Is.EqualTo(ActionState.Succeeded));
        }
    }

    [Test]
    public void Родитель_не_остаётся_активным_когда_тело_бросило_исключение()
    {
        var holder = CreateHolder();
        using var parentCts = new CancellationTokenSource();
        var parent = holder.Register("parent", "Старт", 1, parentCts);

        bool caught;
        try
        {
            try
            {
                throw new InvalidOperationException("источник упал");
            }
            finally
            {
                if (!parentCts.IsCancellationRequested)
                {
                    parent.Fail("Сбой источников (1): src");
                }
            }
        }
        catch (InvalidOperationException)
        {
            caught = true;
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caught, Is.True);
            Assert.That(ContainsAction(holder, parent.Id), Is.False);
            Assert.That(IsCompleted(holder, parent.Id), Is.True);
            Assert.That(parent.State, Is.EqualTo(ActionState.Failed));
        }
    }

    [Test]
    public void Родитель_не_остаётся_активным_когда_синхронизация_отменена()
    {
        var holder = CreateHolder();
        var parentCts = new CancellationTokenSource();
        var parent = holder.Register("parent", "Старт", 2, parentCts);

        try
        {
            parentCts.Cancel();
            throw new OperationCanceledException();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (parentCts.IsCancellationRequested)
            {
                parent.MarkCancelled("Отменено: 0 из 2 источников");
            }
            else
            {
                parent.Finish();
            }
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ContainsAction(holder, parent.Id), Is.False);
            Assert.That(IsCompleted(holder, parent.Id), Is.True);
            Assert.That(parent.State, Is.EqualTo(ActionState.Cancelled));
            Assert.That(parent.Status, Is.EqualTo("Отменено: 0 из 2 источников"));
        }
    }

    [Test]
    public void Отмена_родителя_каскадно_отменяет_детей_с_явной_передачей()
    {
        var holder = CreateHolder();
        var parentCts = new CancellationTokenSource();
        var childCts = new CancellationTokenSource();

        var parent = holder.Register("parent", "Старт", 0, parentCts);
        var child = holder.Register("child", "Старт", 0, childCts, parent: parent);

        parent.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(child.State, Is.EqualTo(ActionState.Cancelled));
            Assert.That(childCts.IsCancellationRequested, Is.True);
            Assert.That(IsCompleted(holder, parent.Id), Is.True);
            Assert.That(IsCompleted(holder, child.Id), Is.True);
        }
    }

    [Test]
    public void Связанный_токен_источника_гаснет_при_отмене_родителя()
    {
        using var parentCts = new CancellationTokenSource();
        using var childCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

        Assert.That(childCts.IsCancellationRequested, Is.False);

        parentCts.Cancel();

        Assert.That(childCts.IsCancellationRequested, Is.True);
    }

    [Test]
    public void Отмена_дочернего_токена_не_трогает_родителя()
    {
        using var parentCts = new CancellationTokenSource();
        using var firstCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);
        using var secondCts = CancellationTokenSource.CreateLinkedTokenSource(parentCts.Token);

        firstCts.Cancel();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstCts.IsCancellationRequested, Is.True);
            Assert.That(parentCts.IsCancellationRequested, Is.False);
            Assert.That(secondCts.IsCancellationRequested, Is.False);
        }
    }
}
