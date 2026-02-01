using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    private readonly Dictionary<string, Coroutine> _taskMap = new Dictionary<string, Coroutine>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // === TaskSequence Integration ===

    public TaskSequencePro CreateSequence() => new TaskSequencePro(this);

    public TaskSequencePro CreatePooledSequence()
    {
        var sequence = TaskSequencePool.Shared.Get();
        sequence.Reset();
        return sequence;
    }

    // === Coroutine Management ===

    public void Run(IEnumerator routine)
    {
        if (routine != null)
            StartCoroutine(routine);
    }

    public void RunTask(string id, IEnumerator routine)
    {
        if (string.IsNullOrEmpty(id) || routine == null)
            return;

        StopTask(id);
        Coroutine coroutine = StartCoroutine(routine);
        _taskMap[id] = coroutine;
    }

    public void StopTask(string id)
    {
        if (_taskMap.TryGetValue(id, out var coroutine) && coroutine != null)
            StopCoroutine(coroutine);

        _taskMap.Remove(id);
    }

    public void StopAllTasks()
    {
        foreach (var coroutine in _taskMap.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        _taskMap.Clear();
    }

    // === Timed + Conditional Execution ===

    public void RunAfter(float delay, Action action)
    {
        Run(DelayRoutine(delay, action));
    }

    public void RunAfter<T>(float delay, Func<T> func, Action<T> onComplete)
    {
        Run(RunAfterRoutine(delay, func, onComplete));
    }

    public void RunUntil(Func<bool> condition, Action onComplete)
    {
        Run(WaitUntilRoutine(condition, onComplete));
    }

    public void RunWhile(Func<bool> condition, float interval, Action onTick, Action onComplete = null)
    {
        Run(WhileRoutine(condition, interval, onTick, onComplete));
    }

    public void RunRepeating(float interval, Action action, string taskId = null)
    {
        var loop = RepeatingRoutine(interval, action);

        if (!string.IsNullOrEmpty(taskId))
            RunTask(taskId, loop);
        else
            Run(loop);
    }

    // === Internal Routines ===

    private IEnumerator DelayRoutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }

    private IEnumerator RunAfterRoutine<T>(float delay, Func<T> func, Action<T> onComplete)
    {
        yield return new WaitForSeconds(delay);
        var result = func != null ? func.Invoke() : default;
        onComplete?.Invoke(result);
    }

    private IEnumerator WaitUntilRoutine(Func<bool> condition, Action onComplete)
    {
        yield return new WaitUntil(condition);
        onComplete?.Invoke();
    }

    private IEnumerator WhileRoutine(Func<bool> condition, float interval, Action onTick, Action onComplete)
    {
        while (condition())
        {
            onTick?.Invoke();
            yield return new WaitForSeconds(interval);
        }

        onComplete?.Invoke();
    }

    private IEnumerator RepeatingRoutine(float interval, Action action)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            action?.Invoke();
        }
    }
}
