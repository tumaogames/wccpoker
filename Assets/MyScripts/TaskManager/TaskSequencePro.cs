using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TaskSequencePro
{
    private readonly TaskManager _manager;
    private readonly Queue<IEnumerator> _steps = new Queue<IEnumerator>();

    private Action _onComplete;
    private Action _onCancel;

    private bool _isRunning;
    private bool _isPaused;
    private bool _isCancelled;
    private bool _isCompleted;
    private bool _hasStarted;

    private Coroutine _currentRoutine;

    private MonoBehaviour CoroutineHost => TaskManager.Instance;

    internal TaskSequencePool Pool { get; set; }

    public TaskSequencePro(TaskManager manager)
    {
        _manager = manager;
    }

    // === Public Properties ===
    public bool IsRunning => _isRunning;
    public bool IsPaused => _isPaused;
    public bool IsCancelled => _isCancelled;
    public bool IsCompleted => _isCompleted;
    public bool HasStarted => _hasStarted;

    // === Chainable Setup ===

    public TaskSequencePro Append(Action action)
    {
        _steps.Enqueue(ActionRoutine(action));
        return this;
    }

    public TaskSequencePro AppendDelayRealtime(float delay)
    {
        _steps.Enqueue(DelayRealtimeRoutine(delay));
        return this;
    }

    private IEnumerator DelayRealtimeRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
    }

    public TaskSequencePro AppendWaitForFixedUpdate()
    {
        _steps.Enqueue(WaitForFixedUpdateRoutine());
        return this;
    }

    private IEnumerator WaitForFixedUpdateRoutine()
    {
        yield return new WaitForFixedUpdate();
    }

    public TaskSequencePro AppendWaitForEndOfFrame()
    {
        _steps.Enqueue(WaitForEndOfFrameRoutine());
        return this;
    }

    private IEnumerator WaitForEndOfFrameRoutine()
    {
        yield return new WaitForEndOfFrame();
    }

    public TaskSequencePro AppendCallback(Action callback) => Append(callback);

    public TaskSequencePro AppendDelay(float delay)
    {
        _steps.Enqueue(DelayRoutine(delay));
        return this;
    }

    public TaskSequencePro AppendWaitUntil(Func<bool> condition)
    {
        _steps.Enqueue(WaitUntilRoutine(condition));
        return this;
    }

    public TaskSequencePro AppendWaitWhile(Func<bool> condition)
    {
        _steps.Enqueue(WaitWhileRoutine(condition));
        return this;
    }

    public TaskSequencePro AppendRoutine(IEnumerator routine)
    {
        _steps.Enqueue(routine);
        return this;
    }

    public TaskSequencePro AppendTween(Tween tween)
    {
        return AppendRoutine(WaitForTween(tween));
    }

    public TaskSequencePro AppendParallel(params IEnumerator[] routines)
    {
        _steps.Enqueue(RunParallelRoutines(routines));
        return this;
    }

    public TaskSequencePro AppendParallel(params Action[] actions)
    {
        IEnumerator[] routines = new IEnumerator[actions.Length];
        for (int i = 0; i < actions.Length; i++)
        {
            routines[i] = ActionRoutine(actions[i]);
        }
        return AppendParallel(routines);
    }

    public TaskSequencePro AppendParallelTweens(params Tween[] tweens)
    {
        IEnumerator[] routines = new IEnumerator[tweens.Length];
        for (int i = 0; i < tweens.Length; i++)
        {
            routines[i] = WaitForTween(tweens[i]);
        }
        return AppendParallel(routines);
    }

    public TaskSequencePro AppendRepeat(int count, Action action, float interval = 0f)
    {
        _steps.Enqueue(RepeatRoutine(count, action, interval));
        return this;
    }

    public TaskSequencePro AppendCondition(Func<IEnumerator> conditionRoutine)
    {
        _steps.Enqueue(conditionRoutine());
        return this;
    }

    public TaskSequencePro AppendIf(bool condition, Action action)
    {
        if (condition)
            Append(action);
        return this;
    }

    public TaskSequencePro AppendDelayIf(bool condition, float delay)
    {
        if (condition)
            AppendDelay(delay);
        return this;
    }

    public TaskSequencePro AppendTweenIf(bool condition, Tween tween)
    {
        if (condition)
            AppendTween(tween);
        return this;
    }

    // === Events ===

    public TaskSequencePro OnComplete(Action callback)
    {
        _onComplete = callback;
        return this;
    }

    public TaskSequencePro OnCancel(Action callback)
    {
        _onCancel = callback;
        return this;
    }

    // === Execution Control ===

    public TaskSequencePro Start()
    {
        if (_isRunning || _hasStarted) return this;

        _isRunning = true;
        _hasStarted = true;
        _isCompleted = false;
        _currentRoutine = CoroutineHost.StartCoroutine(RunSequence());
        return this;
    }

    public void Cancel()
    {
        if (!_isRunning) return;

        _isCancelled = true;
        _isRunning = false;

        if (_currentRoutine != null)
        {
            CoroutineHost.StopCoroutine(_currentRoutine);
            _currentRoutine = null;
        }

        _onCancel?.Invoke();
        ReturnToPoolIfNeeded();
    }

    public void Pause() => _isPaused = true;
    public void Resume() => _isPaused = false;

    public TaskSequencePro Restart()
    {
        Cancel();
        Reset();
        Start();
        return this;
    }

    public TaskSequencePro Reset()
    {
        _steps.Clear();
        _onComplete = null;
        _onCancel = null;
        _isRunning = false;
        _isPaused = false;
        _isCancelled = false;
        _isCompleted = false;
        _hasStarted = false;
        _currentRoutine = null;
        return this;
    }

    // === Internal Execution ===

    private IEnumerator RunSequence()
    {
        while (_steps.Count > 0)
        {
            var step = _steps.Dequeue();

            while (_isPaused)
                yield return null;

            yield return CoroutineHost.StartCoroutine(step);

            if (_isCancelled)
                yield break;
        }

        HandleComplete();
    }

    private void HandleComplete()
    {
        _isRunning = false;
        _isCompleted = true;
        _onComplete?.Invoke();
        ReturnToPoolIfNeeded();
    }

    private void ReturnToPoolIfNeeded()
    {
        Pool?.Return(this);
    }

    // === Routine Builders ===

    private IEnumerator ActionRoutine(Action action)
    {
        action?.Invoke();
        yield return null;
    }

    private IEnumerator DelayRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
    }

    private IEnumerator WaitUntilRoutine(Func<bool> condition)
    {
        yield return new WaitUntil(condition);
    }

    private IEnumerator WaitWhileRoutine(Func<bool> condition)
    {
        yield return new WaitWhile(condition);
    }

    private IEnumerator WaitForTween(Tween tween)
    {
        if (tween == null)
            yield break;

        tween.Play();
        yield return tween.WaitForCompletion();
    }

    private IEnumerator RunParallelRoutines(IEnumerator[] routines)
    {
        int completed = 0;

        foreach (var r in routines)
        {
            CoroutineHost.StartCoroutine(RunAndTrack(r, () => completed++));
        }

        yield return new WaitUntil(() => completed >= routines.Length);
    }

    private IEnumerator RunAndTrack(IEnumerator routine, Action onComplete)
    {
        yield return CoroutineHost.StartCoroutine(routine);
        onComplete?.Invoke();
    }

    private IEnumerator RepeatRoutine(int count, Action action, float interval)
    {
        for (int i = 0; i < count; i++)
        {
            action?.Invoke();
            if (interval > 0f)
                yield return new WaitForSeconds(interval);
            else
                yield return null;
        }
    }
}
