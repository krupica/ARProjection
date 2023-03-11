using Base;
using UnityEngine;
using System.Collections.Generic;
using IO.Swagger.Model;
using TMPro;
using System;
using System.Threading.Tasks;

//[RequireComponent(typeof(OutlineOnClick))]
//[RequireComponent(typeof(Target))]
public class ActionPoint2D : Base.ActionPoint {

    //public GameObject Instance;  

    public override bool BreakPoint {
        get => base.BreakPoint;
        set {
            base.BreakPoint = value;
        }
    }

    public override Vector3 GetScenePosition() {
        //Vector3 kinectPoint = GameManager.Instance.kinectToWorld.MultiplyPoint(DataHelper.PositionToVector3(Data.Position));
        //Vector3 rotatedPoint = GameManager.Instance.calibrationData.rotation.MultiplyPoint(kinectPoint);
        //Vector3 transformedPoint = rotatedPoint + GameManager.Instance.calibrationData.translation;

        //return transformedPoint;
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Position));
    }

    public override Quaternion GetSceneOrientation() {
        return Quaternion.identity;
    }

    /// <summary>
    /// Changes size of shpere representing action point
    /// </summary>
    /// <param name="size"><0; 1> - 0 means invisble, 1 means 10cm in diameter</param>
    public override void SetSize(float size) {
        //Visual.transform.localScale = new Vector3(size / 10, size / 10, size / 10);
    }

    public override (List<string>, Dictionary<string, string>) UpdateActionPoint(IO.Swagger.Model.ActionPoint projectActionPoint) {
        (List<string>, Dictionary<string, string>) result = base.UpdateActionPoint(projectActionPoint);
        //ActionPointName.text = projectActionPoint.Name;
        return result;
    }
}
