using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public class ActionHolder(ILogger<ActionHolder> logger)
{
    public Dictionary<Guid, RunningAction> Actions = new Dictionary<Guid, RunningAction>();

    public RunningAction Register(string name, string status, int progressMax, CancellationTokenSource ctx)
    {
        var id = Guid.NewGuid();
        var act = new RunningAction
        {
            Id = id,
            Name = name,
            Status = status,
            ProgressMax = progressMax,
            CancellationTokenSource = ctx,
            Holder = this,
        };
        Actions.Add(id, act);
        return act;
    }

    public void SetStatus(Guid id, string value)
    {
        Actions[id].Status = value;
    }

    public void ProgressPlus(Guid id)
    {
        Actions[id].ProgressValue++;
    }

    public void Cancel(Guid id)
    {
        Actions[id].CancellationTokenSource.Cancel();
        Actions.Remove(id);
    }

    public class RunningAction
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public int ProgressValue { get; set; }
        public int ProgressMax { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ActionHolder Holder { get; internal set; }

        public void Cancel()
        {
            Status = "Отменено";
            Holder.Cancel(Id);
        }

        public void ProgressPlus()
        {
            Holder.ProgressPlus(Id);
        }

        public void Finish()
        {
            Status = "Выполнено";
            Holder.Cancel(Id);
        }
    }
}
