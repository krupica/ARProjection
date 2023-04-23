using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Base;
using IO.Swagger.Model;
using System;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;
using Assets.Scripts.ARClasses;

public class KinectAzure : ActionObject
{
    public override Vector3 GetScenePosition()
    {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Pose.Position));
    }

    public override void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger)
    {
        base.ActionObjectUpdate(actionObjectSwagger);
        ResetPosition();
        ProjectionManager.Instance.UpdateProjectorTransform();

    }

    public override void CreateModel()
    {
        
    }
}
