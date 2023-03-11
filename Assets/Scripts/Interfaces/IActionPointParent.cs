using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionPointParent
{
    string GetName();

    string GetId();

    bool IsActionObject();

    Base.ActionObject GetActionObject();

    Transform GetTransform();

    GameObject GetGameObject();

    Transform GetSpawnPoint();

}
