﻿using RingLib.StateMachine;
using RingLib.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DreamEchoesCore.Entities.Enemies.Seer.SeerStateMachine;

internal class GrubFSM : StateMachine
{
    [State]
    private IEnumerator<Transition> Begin()
    {
        yield return new WaitFor { Seconds = 10 };
        Destroy(gameObject);
    }

    public GrubFSM() : base(
        startState: nameof(Begin),
        globalTransitions: [])
    { }
}

internal partial class SeerStateMachine : EntityStateMachine
{
    private IEnumerator<Transition> TeleSlashSlash()
    {
        if (!FacingTarget())
        {
            yield return new CoroutineTransition
            {
                Routine = Turn()
            };
        }
        var velocityX = (Target().Position().x - Position.x);
        velocityX *= Config.SlashVelocityXScale * 2;
        var minVelocityX = Config.ControlledSlashVelocityX;
        if (Mathf.Abs(velocityX) < minVelocityX)
        {
            velocityX = Mathf.Sign(velocityX) * minVelocityX;
        }
        Velocity = Vector2.zero;

        IEnumerator<Transition> Slash(string slash)
        {
            if (slash.EndsWith("1"))
            {
                speak.PlayOneShot(animator.Slash1Words);
            }
            else if (slash.EndsWith("2"))
            {
                speak.PlayOneShot(animator.Slash2Words);
            }
            else if (slash.EndsWith("3"))
            {
                speak.PlayOneShot(animator.Slash3Words);
            }
            if (!FacingTarget())
            {
                velocityX *= -1;
                Velocity *= -1;
                yield return new CoroutineTransition
                {
                    Routine = Turn()
                };
            }
            var previousVelocityX = Velocity.x;
            Transition updater(float normalizedTime)
            {
                var currentVelocityX = Mathf.Lerp(previousVelocityX, velocityX, normalizedTime);
                Velocity = new Vector2(currentVelocityX, 0);
                return new NoTransition();
            }
            yield return new CoroutineTransition
            {
                Routine = animator.PlayAnimation(slash, updater)
            };
        }
        foreach (var slash in new string[] { "Slash1", "Slash2" })
        {
            yield return new CoroutineTransition
            {
                Routine = Slash(slash)
            };
        }

        Velocity = Vector2.zero;
    }

    private RandomSelector<string> teleSlashRandomSelector = new([
        new(value: nameof(Dash), weight: 1, maxCount: 2, maxMiss: 5),
        new(value: nameof(Slash), weight: 1, maxCount: 2, maxMiss: 5),
        new(value: nameof(Hug), weight: 1, maxCount: 2, maxMiss: 5),
        // new(value: nameof(TeleSlash), weight: 1, maxCount: 2, maxMiss: 8),
        // new(value: nameof(Laser), weight: 0.6f, maxCount: 1, maxMiss: 8),
    ]);

    [State]
    private IEnumerator<Transition> TeleSlash()
    {
        speak.PlayOneShot(animator.TeleSlashWord);
        var currentY = Position.y;
        var heroX = Target().Position().x;
        var candidateXs = new float[] { heroX - Config.TeleSlashX, heroX + Config.TeleSlashX };
        float candidateX = 0;
        while (true)
        {
            candidateX = candidateXs[Random.Range(0, 2)];
            if (candidateX > minX && candidateX < maxX)
            {
                break;
            }
        }
        var candidateY = currentY + Config.TeleSlashY;

        if (!FacingTarget())
        {
            yield return new CoroutineTransition { Routine = Turn() };
        }
        Velocity = Vector2.zero;
        yield return new CoroutineTransition { Routine = animator.PlayAnimation("Tele1") };

        Position = new Vector2(candidateX, candidateY);
        Rigidbody2D.gravityScale = 0;
        if (!FacingTarget())
        {
            yield return new CoroutineTransition { Routine = Turn() };
        }
        yield return new CoroutineTransition { Routine = animator.PlayAnimation("Tele2") };

        heroX = Target().Position().x;
        candidateXs = new float[] { heroX - Config.TeleSlashXClose, heroX + Config.TeleSlashXClose };
        bool valid(float x)
        {
            return x > minX && x < maxX;
        }
        candidateXs = candidateXs.ToList().Where(valid).ToArray();
        if (candidateXs.Length == 1)
        {
            candidateX = candidateXs[0];
        }
        else
        {
            if (Direction() > 0)
            {
                candidateX = candidateXs[0];
            }
            else
            {
                candidateX = candidateXs[1];
            }
        }
        candidateY = currentY;
        Rigidbody2D.gravityScale = Config.GravityScale;
        Position = new Vector2(candidateX, candidateY);
        if (!FacingTarget())
        {
            yield return new CoroutineTransition { Routine = Turn() };
        }
        yield return new CoroutineTransition { Routine = animator.PlayAnimation("Tele3") };

        Velocity = Vector2.zero;
        yield return new ToState { State = teleSlashRandomSelector.Get() };

        yield return new CoroutineTransition
        {
            Routine = TeleSlashSlash()
        };

        Velocity = Vector2.zero;
        yield return new ToState { State = nameof(Idle) };
    }
}
