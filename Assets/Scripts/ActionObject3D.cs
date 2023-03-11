using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Base;
using IO.Swagger.Model;
using System;
using System.Threading.Tasks;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Target))]
public class ActionObject3D : ActionObject
{
    [SerializeField]
    public GameObject CubePrefab;
    public GameObject Model;

    public GameObject CylinderPrefab, SpherePrefab;


    protected void Start()
    {
        transform.localScale = new Vector3(1f, 1f, 1f);
    }


    public override Vector3 GetScenePosition()
    {
        return TransformConvertor.ROSToUnity(DataHelper.PositionToVector3(Data.Pose.Position));
    }

    public override void SetScenePosition(Vector3 position)
    {
        Data.Pose.Position = DataHelper.Vector3ToPosition(TransformConvertor.UnityToROS(position));
    }

    public override Quaternion GetSceneOrientation()
    {
        return TransformConvertor.ROSToUnity(DataHelper.OrientationToQuaternion(Data.Pose.Orientation));
    }

    public override void SetSceneOrientation(Quaternion orientation)
    {
        Data.Pose.Orientation = DataHelper.QuaternionToOrientation(TransformConvertor.UnityToROS(orientation));
    }

    public override void UpdateObjectName(string newUserId)
    {
        base.UpdateObjectName(newUserId);
    }

    public override void ActionObjectUpdate(IO.Swagger.Model.SceneObject actionObjectSwagger)
    {
        base.ActionObjectUpdate(actionObjectSwagger);
        ResetPosition();
    }

    public override void SetVisibility(float value, bool forceShaderChange = false)
    {
        
    }

    public override void Show()
    {
    }

    public override void Hide()
    {
    }

    public override void SetInteractivity(bool interactivity)
    {
    }

    public override void ActivateForGizmo(string layer)
    {
    }

    public override void CreateModel(CollisionModels customCollisionModels = null)
    {

        if (ActionObjectMetadata.ObjectModel == null || ActionObjectMetadata.ObjectModel.Type == IO.Swagger.Model.ObjectModel.TypeEnum.None)
        {
            //Model = Instantiate(CubePrefab, Visual.transform);
            //Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
        }
        else
        {
            switch (ActionObjectMetadata.ObjectModel.Type)
            {
                case IO.Swagger.Model.ObjectModel.TypeEnum.Box:
                    Model = Instantiate(CubePrefab, transform);

                    if (customCollisionModels == null)
                    {
                        Model.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Box.SizeX, (float)ActionObjectMetadata.ObjectModel.Box.SizeY, (float)ActionObjectMetadata.ObjectModel.Box.SizeZ));
                    }
                    else
                    {
                        foreach (IO.Swagger.Model.Box box in customCollisionModels.Boxes)
                        {
                            if (box.Id == ActionObjectMetadata.Type)
                            {
                                Model.transform.localScale = TransformConvertor.ROSToUnityScale(new Vector3((float)box.SizeX, (float)box.SizeY, (float)box.SizeZ));
                                break;
                            }
                        }
                    }
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Cylinder:
                    Model = Instantiate(CylinderPrefab, transform);
                    if (customCollisionModels == null)
                    {
                        Model.transform.localScale = new Vector3((float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Height / 2, (float)ActionObjectMetadata.ObjectModel.Cylinder.Radius);
                    }
                    else
                    {
                        foreach (IO.Swagger.Model.Cylinder cylinder in customCollisionModels.Cylinders)
                        {
                            if (cylinder.Id == ActionObjectMetadata.Type)
                            {
                                Model.transform.localScale = new Vector3((float)cylinder.Radius, (float)cylinder.Height, (float)cylinder.Radius);
                                break;
                            }
                        }
                    }
                    break;
                case IO.Swagger.Model.ObjectModel.TypeEnum.Sphere:
                    Model = Instantiate(SpherePrefab, transform);
                    if (customCollisionModels == null)
                    {
                        Model.transform.localScale = new Vector3((float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius);
                    }
                    else
                    {
                        foreach (IO.Swagger.Model.Sphere sphere in customCollisionModels.Spheres)
                        {
                            if (sphere.Id == ActionObjectMetadata.Type)
                            {
                                Model.transform.localScale = new Vector3((float)sphere.Radius, (float)sphere.Radius, (float)sphere.Radius);
                                break;
                            }
                        }
                    }
                    break;
                case ObjectModel.TypeEnum.Mesh:
                    //MeshImporter.Instance.OnMeshImported += OnModelLoaded;
                    //MeshImporter.Instance.LoadModel(ActionObjectMetadata.ObjectModel.Mesh, GetId());

                    Model = Instantiate(CubePrefab, transform);
                    Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                    break;
                default:
                    Model = Instantiate(CubePrefab, transform);
                    Model.transform.localScale = new Vector3(0.05f, 0.01f, 0.05f);
                    break;
            }
        }

        gameObject.GetComponent<BindParentToChild>().ChildToBind = Model;
    }

    public override GameObject GetModelCopy()
    {
        GameObject model = Instantiate(Model);
        model.transform.localScale = Model.transform.localScale;
        return model;
    }

    /// <summary>
    /// For meshes...
    /// </summary>
    /// <param name="assetLoaderContext"></param>
    //public void OnModelLoaded(object sender, ImportedMeshEventArgs args)
    //{
    //    if (args.Name != this.GetId())
    //        return;

    //    bool outlineWasHighlighted = outlineOnClick.Highlighted;

    //    if (Model != null)
    //    {
    //        outlineOnClick.UnHighlight();
    //        outlineOnClick.ClearRenderers();

    //        Model.SetActive(false);
    //        Destroy(Model);
    //    }

    //    Model = args.RootGameObject;

    //    Model.gameObject.transform.parent = Visual.transform;
    //    Model.gameObject.transform.localPosition = Vector3.zero;
    //    Model.gameObject.transform.localRotation = Quaternion.identity;

    //    gameObject.GetComponent<BindParentToChild>().ChildToBind = Model;

    //    foreach (Renderer child in Model.GetComponentsInChildren<Renderer>(true))
    //    {
    //        //child.gameObject.AddComponent<OnClickCollider>().Target = gameObject;
    //        //child.gameObject.AddComponent<MeshCollider>();
    //    }

    //    aoRenderers.Clear();
    //    Colliders.Clear();
    //    aoRenderers.AddRange(Model.GetComponentsInChildren<Renderer>(true));
    //    Colliders.AddRange(Model.GetComponentsInChildren<MeshCollider>(true));
    //    outlineOnClick.InitRenderers(aoRenderers);
    //    outlineOnClick.InitMaterials();
    //    SetVisibility(visibility, forceShaderChange: true);

    //    if (outlineWasHighlighted)
    //    {
    //        outlineOnClick.Highlight();
    //        //if (SelectorMenu.Instance.ManuallySelected)
    //        //{
    //        //    DisplayOffscreenIndicator(true);
    //        //}
    //    }

    //    //MeshImporter.Instance.OnMeshImported -= OnModelLoaded;
    //}

    /// <summary>
    /// For meshes...
    /// </summary>
    /// <param name="obj"></param>
    //private void OnModelLoadError(IContextualizedError obj)
    //{
    //    Notifications.Instance.ShowNotification("Unable to show mesh " + this.GetName(), obj.GetInnerException().Message);
    //}


    public string GetObjectTypeName()
    {
        return "Action object";
    }

    public override void UpdateModel()
    {
        if (ActionObjectMetadata.ObjectModel == null)
            return;
        Vector3? dimensions = null;
        switch (ActionObjectMetadata.ObjectModel.Type)
        {
            case ObjectModel.TypeEnum.Box:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Box.SizeX, (float)ActionObjectMetadata.ObjectModel.Box.SizeY, (float)ActionObjectMetadata.ObjectModel.Box.SizeZ));
                break;
            case ObjectModel.TypeEnum.Sphere:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius, (float)ActionObjectMetadata.ObjectModel.Sphere.Radius));
                break;
            case ObjectModel.TypeEnum.Cylinder:
                dimensions = TransformConvertor.ROSToUnityScale(new Vector3((float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Radius, (float)ActionObjectMetadata.ObjectModel.Cylinder.Height));
                break;

        }
        if (dimensions != null)
            Model.transform.localScale = new Vector3(dimensions.Value.x, dimensions.Value.y, dimensions.Value.z);

    }
}
