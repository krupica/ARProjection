using Assets.Scripts.Classes;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DataManager : MonoBehaviour
{
    public static RecievedData Data { get; set; }

    public GameObject ActionPointPrefab;
    public List<ActionPoint> actionPoints;

    private Matrix4x4 rotationMatrix;
    private Vector3 translation;

    

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("START");
        actionPoints= new List<ActionPoint>();
        FillMatrix();
    }

    // Update is called once per frame
    void Update()
    {        
        if (Input.GetButtonDown("Fire1"))
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos = Camera.main.ScreenToWorldPoint(mousePos);
            mousePos.z = 0;
            {
                AddActionPoint(mousePos);
            }
        }
    }

    void AddActionPoint(Vector3 location)
    {
        GameObject actionPoint = Instantiate(ActionPointPrefab);
        ActionPoint tmp = new ActionPoint(location, actionPoint, rotationMatrix, translation);
        actionPoints.Add(tmp);
    }

    public void MovePoint(Vector3 change, int id)
    {
        actionPoints[id].Move(change);
    }

    public void RemoveActionPoint(int id)
    {
        actionPoints.RemoveAt(id);
    }

    void FillMatrix()
    {
        rotationMatrix = new Matrix4x4();
        rotationMatrix.SetRow(0, new Vector4((float)9.9995961310770576e-01, (float)8.9872990011441688e-03, (float)2.4701242572249633e-05, 0));
        rotationMatrix.SetRow(1, new Vector4((float)-8.9613393813132575e-03, (float)9.9685483491258131e-01, (float)7.8740920161649047e-02, 0));
        rotationMatrix.SetRow(2, new Vector4((float)6.8304464003145823e-04, (float)-7.8737961416805169e-02, (float)9.9689511328020131e-01, 0));
        rotationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        translation = new Vector3((float)8.5737214348908466e+01, (float)-6.3045718819563251e+02, (float)-1.0084398390544085e+02);
    }
}
