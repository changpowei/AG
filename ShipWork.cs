using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipWork : MonoBehaviour
{
    public string shipName = "";
    public bool isKnown = false;
    public bool startSimulator = false;

    public Material material_unknown;
    public Material material_known;
    public MeshRenderer knownSign;
    public ShipNode baseNode;

    public float shipSpeed = 30.0f;

    public float distance = 0.0f;
    public List<DangerRange> dangerRanges = new List<DangerRange>();

    private Transform self_trans;
    private Transform target_trans;

    public float searchRange = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        self_trans = this.transform;

        if (target_trans == null)
            target_trans = GameObject.FindObjectOfType<Controller>().transform;

        if (knownSign != null)
        {
            if (isKnown)
                knownSign.material = material_known;
            else
                knownSign.material = material_unknown;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RF"))
        {
            var missile = FindObjectOfType<Controller>();

            if ((!isKnown) && (Vector3.Distance(target_trans.position, this.transform.position)<=28000))
            {
                missile.SettingAvoidPath(this.transform.position, searchRange);
                SetKnown(true);
            }

            if ((missile.enmyTarget == null) &&(this.name == "CVLL"))            
                missile.enmyTarget = this;                  
        }
        else if(other.CompareTag("Missile"))
        {
            startSimulator = false;
            other.GetComponent<Controller>().Broken();
            Debug.Log("The Ship [" + shipName + "] is be destroy!!");
        }
    }

    // Update is called once per frame
    public void SetKnown(bool setKnown)
    {
        isKnown = setKnown;
        if (isKnown)
            knownSign.material = material_known;
        else
            knownSign.material = material_unknown;
    }

    private void OnGUI()
    {
        if (startSimulator)
        {
            if (isKnown)
            {
                var tar_pos = target_trans.position;
                var self_pos = self_trans.position;
                var indanger = false;
                distance = Vector2.Distance(new Vector2(tar_pos.x, tar_pos.z), new Vector2(self_pos.x, self_pos.z));
                if (distance > 1000.0f)
                {
                    baseNode.d_value.text = (distance / 1000.0f).ToString("0.000");
                    baseNode.d_stand.text = "Km";
                }
                else
                {
                    baseNode.d_value.text = distance.ToString("0.000");
                    baseNode.d_stand.text = "m";
                }
                if (dangerRanges.Count > 0)
                {
                    for (int i = 0; i < dangerRanges.Count; i++)
                    {
                        var max_d = dangerRanges[i].max_distance;
                        var min_d = dangerRanges[i].min_diatance;
                        var max_value = dangerRanges[i].max_dnagerValue;
                        var min_value = dangerRanges[i].min_dnagerValue;

                        if ((distance <= max_d) && (distance > min_d)) //Distance is in Range
                        {
                            indanger = true;
                            baseNode.d_value.color = dangerRanges[i].danagerColor.Evaluate((max_d - distance) / (max_d - min_d));
                            baseNode.a_value.color = dangerRanges[i].danagerColor.Evaluate((max_d - distance) / (max_d - min_d));
                            baseNode.a_value.text = (((max_d - distance) / (max_d - min_d)) * (min_value - max_value) + max_value).ToString();
                        }
                    }
                }
                if (!indanger)
                {
                    baseNode.d_value.color = Color.white;
                    baseNode.a_value.color = Color.white;
                    baseNode.a_value.text = "0";
                }
            }
        } 
    }

    public void StartSimulator()
    {
        if (startSimulator)
            return;
        startSimulator = true;
        StartCoroutine(SimulatorMoving());
    }

    public void End()
    {
        startSimulator = false;
        baseNode.Reset();
    }

    IEnumerator SimulatorMoving()
    {
        while (startSimulator)
        {
            yield return null;
            this.transform.Translate(0.0f, 0.0f, (shipSpeed * 0.5144f) / 60.0f * Time.timeScale);
        }
    }
}

[System.Serializable]
public class DangerRange 
{
    public Gradient danagerColor;
    public float max_distance;
    public float min_diatance;
    public float max_dnagerValue;
    public float min_dnagerValue;
}
