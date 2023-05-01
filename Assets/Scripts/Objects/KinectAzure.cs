using UnityEngine;
using Base;
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

    public override void DeleteActionObject()
    {
        RemoveActionPoints();
        // Remove this ActionObject reference from the scene ActionObject list
        SceneManager.Instance.ActionObjects.Remove(Data.Id);
        Destroy(gameObject);
        ProjectionManager.Instance.DestroyProjection();
    }

    public override Quaternion GetSceneOrientation()
    {
        return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
    }

    public override void CreateModel()
    {
        
    }
}
