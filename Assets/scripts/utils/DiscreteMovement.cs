﻿using System;
using UnityEngine;

public static class DiscreteMovement
{
    public enum MovingDirection
    {
        forward,
        forward_right,
        right,
        backward_right,
        backward,
        backward_left,
        left,
        forward_left,
    }

    public static readonly Vector3 rot_forward = new Vector3(0, 0, 0);
    public static readonly Vector3 rot_forward_right = new Vector3(0, 45, 0);
    public static readonly Vector3 rot_right = new Vector3(0, 90, 0);
    public static readonly Vector3 rot_backward_right = new Vector3(0, 135, 0);
    public static readonly Vector3 rot_backward = new Vector3(0, 180, 0);
    public static readonly Vector3 rot_backward_left = new Vector3(0, 225, 0);
    public static readonly Vector3 rot_left = new Vector3(0, 270, 0);
    public static readonly Vector3 rot_forward_left = new Vector3(0, 315, 0);

    public static readonly Vector3 dir_backward = -Vector3.forward;
    public static readonly Vector3 dir_left = -Vector3.right;
    public static readonly Vector3 dir_forward_right = new Vector3(1, 0, 1);
    public static readonly Vector3 dir_backward_left = -dir_forward_right;
    public static readonly Vector3 dir_forward_left = new Vector3(-1, 0, 1);
    public static readonly Vector3 dir_backward_right = -dir_forward_left;


    public static Vector3 GetDirectionVector(MovingDirection direction)
    {
        switch (direction)
        {
            case MovingDirection.forward:           // forward
                return Vector3.forward;
            case MovingDirection.forward_right:     // forward right
                return dir_forward_right;
            case MovingDirection.right:             // right
                return Vector3.right;
            case MovingDirection.backward_right:    // backward right
                return dir_backward_right;
            case MovingDirection.backward:          // backward
                return dir_backward;
            case MovingDirection.backward_left:     // backward left
                return dir_backward_left;
            case MovingDirection.left:              // left
                return dir_left;
            case MovingDirection.forward_left:      // forward left
                return dir_forward_left;
            default:
                throw new SystemException("Direction vector for direction: " + direction + " not defined");
        }
    }

    public static Vector3 GetRotationVector(MovingDirection direction)
    {
        switch (direction)
        {
            case MovingDirection.forward:           // forward
                return rot_forward;
            case MovingDirection.forward_right:     // forward right
                return rot_forward_right;
            case MovingDirection.right:             // right
                return rot_right;
            case MovingDirection.backward_right:    // backward right
                return rot_backward_right;
            case MovingDirection.backward:          // backward
                return rot_backward;
            case MovingDirection.backward_left:     // backward left
                return rot_backward_left;
            case MovingDirection.left:              // left
                return rot_left;
            case MovingDirection.forward_left:      // forward left
                return rot_forward_left;
            default:
                throw new SystemException("Rotation vector for direction: " + direction + " not defined");
        }
    }

    public static void RotateXZDiscrete45(Rigidbody rigidbody, Vector3 movement)
    {
        DiscreteMovement.MovingDirection direction;

        // no direction
        if (movement.x == 0 && movement.z == 0)
        {
            return;
        }
        // forward
        if (movement.x == 0 && movement.z > 0)
        {
            direction = DiscreteMovement.MovingDirection.forward;
        }
        // forward right
        else if (movement.x > 0 && movement.z > 0)
        {
            direction = DiscreteMovement.MovingDirection.forward_right;
        }
        // right
        else if (movement.x > 0 && movement.z == 0)
        {
            direction = DiscreteMovement.MovingDirection.right;
        }
        // backward right
        else if (movement.x > 0 && movement.z < 0)
        {
            direction = DiscreteMovement.MovingDirection.backward_right;
        }
        // backward
        else if (movement.x == 0 && movement.z < 0)
        {
            direction = DiscreteMovement.MovingDirection.backward;
        }
        // backward left
        else if (movement.x < 0 && movement.z < 0)
        {
            direction = DiscreteMovement.MovingDirection.backward_left;
        }
        // left
        else if (movement.x < 0 && movement.z == 0)
        {
            direction = DiscreteMovement.MovingDirection.left;
        }
        // forward left
        else if (movement.x < 0 && movement.z > 0)
        {
            direction = DiscreteMovement.MovingDirection.forward_left;
        }
        else
            return;

        RotateDiscrete(rigidbody, direction);
    }

    public static void RotateDiscrete(Rigidbody rigidbody, DiscreteMovement.MovingDirection direction)
    {
        rigidbody.MoveRotation(Quaternion.Euler(DiscreteMovement.GetRotationVector(direction)));
    }

    public static void MoveDiscrete(Rigidbody rigidbody, DiscreteMovement.MovingDirection direction, float speed)
    {
        CommonUtils.Move(rigidbody, DiscreteMovement.GetDirectionVector(direction), speed);
    }

}