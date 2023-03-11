using System;
using Base;
using UnityEngine;
using System.Threading.Tasks;

public abstract class StartEndAction : Base.Action {
    public Renderer Visual;
    public GameObject VisualRoot;

    protected string playerPrefsKey;
    //[SerializeField]
    //protected OutlineOnClick outlineOnClick;
    public GameObject ModelPrefab;


    public virtual void Init(IO.Swagger.Model.Action projectAction, Base.ActionMetadata metadata, Base.ActionPoint ap, IActionProvider actionProvider, string actionType) {
        base.Init(projectAction, metadata, ap, actionProvider);

        if (!Base.ProjectManager.Instance.ProjectMeta.HasLogic) {
            Destroy(gameObject);
            return;
        }
        playerPrefsKey = "project/" + ProjectManager.Instance.ProjectMeta.Id + "/" + actionType;

    }

    public void SavePosition() {
        PlayerPrefsHelper.SaveVector3(playerPrefsKey, transform.localPosition);
    }

    public void OnHoverStart() {
        if (GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.Normal &&
            GameManager.Instance.GetEditorState() != GameManager.EditorStateEnum.SelectingAction) {
            if (GameManager.Instance.GetEditorState() == GameManager.EditorStateEnum.InteractionDisabled) {
                if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.PackageRunning)
                    return;
            } else {
                return;
            }
        }
        if (GameManager.Instance.GetGameState() != GameManager.GameStateEnum.ProjectEditor &&
            GameManager.Instance.GetGameState() != GameManager.GameStateEnum.PackageRunning) {
            return;
        }
        //outlineOnClick.Highlight();
        NameText.gameObject.SetActive(true);
        //if (SelectorMenu.Instance.ManuallySelected) {
        //}
    }


    public async Task<RequestResult> Movable() {
        return new RequestResult(true);
    }

    public bool HasMenu() {
        return false;
    }

    public void StartManipulation() {
        throw new NotImplementedException();
    }

    public async Task<bool> WriteUnlock() {
        return true;
    }

    public async Task<bool> WriteLock(bool lockTree) {
        return true;
    }

    protected void OnObjectLockingEvent(object sender, ObjectLockingEventArgs args) {
        return;
    }

    public void OnObjectLocked(string owner) {
        return;
    }

    public void OnObjectUnlocked() {
        return;
    }

    public string GetName() {
        return Data.Name;
    }    

    public void OpenMenu() {
        throw new NotImplementedException();
    }

    //public async Task<RequestResult> Removable() {
    //    return new RequestResult(false, GetObjectTypeName() + " could not be removed");
    //}

    public void Remove() {
        throw new NotImplementedException();
    }

    public Task Rename(string name) {
        throw new NotImplementedException();
    }

    public GameObject GetModelCopy() {
        return Instantiate(ModelPrefab);
    }

    public void EnableVisual(bool enable) {
        VisualRoot.SetActive(enable);
    }
}
