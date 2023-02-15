using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionPoint
{

    public Vector3 Location;
    public GameObject Instance;

    private Matrix4x4 rotationMatrix;
    private Vector3 translation;

    public ActionPoint(Vector3 location, GameObject insatance, Matrix4x4 rotation, Vector3 translation)
    {
        Location = location;
        Instance = insatance;
        rotationMatrix = rotation;
        this.translation= translation;
        Instance.transform.position = location;
        
        CalcPosition();
    }

    public void Move(Vector3 change)
    {
        Location.x += change.x;
        Location.y += change.y;
        Location.z += change.z;
        CalcPosition();
    }

    public void CalcPosition()
    {
        Vector3 rotatedPoint = rotationMatrix.MultiplyPoint(Location);
        Vector3 transformedPoint = rotatedPoint + translation;
        Instance.transform.position = transformedPoint;
    }
}
