using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    int pointingDir = 1;
    Vector2 target;
    Vector2 targetVec;
    Vector2 targetVecNorm;
    float speed = 0;
    float turnSpeed = 0;
    float turnDiff = 0;
    float targetRot;
    bool playerOwned = false;
    public bool spiningWeapon = false;
    bool traveling = false;
    bool locked = false;

    public void setPointDir(bool pOwned)
    {
        playerOwned = pOwned;
        if (playerOwned)
        {
            pointingDir = 1;
        }
        else
        {
            pointingDir = -1;
            transform.Rotate(0, 0, 180);
        }
    }

    public void setTarget(Vector2 trgt, float sp, float tSp, bool playerOwned)
    {
        target = trgt;
        targetVec = new Vector2(target.x - transform.position.x, target.y - transform.position.y);
        targetVecNorm = targetVec.normalized;

        speed = sp;
        turnSpeed = tSp;// * -Mathf.Sign(targetVec.x);

        targetRot = Vector2.SignedAngle(new Vector2(0,1), targetVec);
        if (targetRot < 0)
        {
            targetRot = 360 + targetRot;
        }

        if (targetRot <= 180)
        {
            turnSpeed = tSp * pointingDir;
        }
        else
        {
            turnSpeed = -tSp * pointingDir;
        }
        turnDiff = 2 * Mathf.Abs(turnSpeed);
    }

    public void shoot()
    {
        //locked = true;
        traveling = true;
        transform.rotation = Quaternion.Euler(0, 0, targetRot);
    }

    public float getDistance()
    {
        return targetVec.magnitude;
    }

    private void FixedUpdate()
    {
        if (traveling)
        {
            transform.position = new Vector3(transform.position.x + targetVecNorm.x*speed, transform.position.y + targetVecNorm.y*speed, transform.position.z);
            if (spiningWeapon)
            {
                transform.Rotate(0, 0, 10*turnSpeed);
            }
        }
        else
        {
            if (!locked)
            {
                transform.Rotate(0, 0, turnSpeed);
                if (Mathf.Abs(transform.rotation.eulerAngles.z - targetRot) <= turnDiff)
                {
                    locked = true;
                }
            }
        }
        
    }
}
