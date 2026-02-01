using System.Collections.Generic;

public class TaskSequencePool
{
    public static readonly TaskSequencePool Shared = new TaskSequencePool();

    private readonly Stack<TaskSequencePro> _pool = new Stack<TaskSequencePro>();
    private readonly TaskManager _manager;

    public TaskSequencePool()
    {
        _manager = TaskManager.Instance;
    }

    public TaskSequencePro Get()
    {
        TaskSequencePro sequence;

        if (_pool.Count > 0)
        {
            sequence = _pool.Pop();
        }
        else
        {
            sequence = new TaskSequencePro(_manager);
        }

        sequence.Pool = this;
        return sequence;
    }

    public void Return(TaskSequencePro sequence)
    {
        sequence.Reset(); // Ensure clean state
        sequence.Pool = null;
        _pool.Push(sequence);
    }
}
