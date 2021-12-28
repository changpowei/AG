using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public bool work = false;
    public SettingControl setting;

    public float speed = 272.0f;

    [Range(-45.0f,45.0f)]
    public float right = 0.0f;

    [Range(-45.0f, 45.0f)]
    public float up = 0.0f;

    public float upChangeSpped = 0.1f;
    public float rightChangeSpped = 0.1f;

    public float stand_Speed = 238.0f;
    public float stand_MaxRotate = 38.6f;
    public float stand_MaxCenterG = 0.8f;
    public float stand_HalfLength = 7225.0f;

    public float stand_OneRoundTime = 0.0f;
    public float stand_PerSecondRotate = 0.0f;
    public float stand_RotateCoefficient = 0.0f;

    public float right_coefficient = (float)((360 / ((2 * Mathf.PI * 7225) / 0.8)) / 38.6);

    public Animator animator;

    public Text text_x;
    public Text text_x_stand;
    public Text text_z;
    public Text text_z_stand;
    public Text text_h;
    public Text text_h_stand;
    public Text text_v;

    private float x_value;
    private float z_value;
    private float h_value;

    public Text text_r;
    public Image imagePoint;

    public bool limitFPS = false;

    public bool startSimulator = false;

    public bool navigate = false;
    public Vector3[] navigateRoute;
    public int nav_pointer = 1;
    public int frameCount = 0;
    public ShipWork enmyTarget;

    public GameObject model_RF;
    public bool work_RF = true;

    public LineRenderer path_Gone;

    public GameObject pathGiver;
    public GameObject pathTrace;

    private void Awake()
    {
        if (limitFPS)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        stand_OneRoundTime = (2 * Mathf.PI * stand_HalfLength) / stand_Speed;
        stand_PerSecondRotate = 360 / stand_OneRoundTime;
        stand_RotateCoefficient = stand_PerSecondRotate / stand_MaxRotate;
    }

    public void StartSimulator()
    {
        if (startSimulator)
            return;
        startSimulator = true;
        path_Gone.SetPosition(0, this.transform.position);
        path_Gone.SetPosition(1, this.transform.position);
        StartCoroutine(PathRecord());
        StartCoroutine(RFWork());
        if(navigate)
            StartCoroutine(SimulatorNavigate());
        else
            StartCoroutine(SimulatorMoving());
    }


    public void Work()
    {
        work = true;
    }

    public void End()
    {
        startSimulator = false;

        if (setting != null)
        {
            setting.Reset();
            path_Gone.positionCount = 2;
            var defaultPoints = new Vector3[2];
            defaultPoints[0] = this.transform.position;
            defaultPoints[1] = this.transform.position;
            path_Gone.SetPositions(defaultPoints);

            if (pathTrace.transform.childCount == 0)
                return;

            for(int i = pathTrace.transform.childCount-1; i >0; i--)
                GameObject.Destroy(pathTrace.transform.GetChild(i).gameObject);           
        }
    }

    public void Broken()
    {
        if (startSimulator)
        {
            startSimulator = false;
            speed = 0;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Ship")
            Destroy(other.gameObject);
    }

    IEnumerator SimulatorMoving()
    {
        while (startSimulator)
        {
            yield return null;
            if (enmyTarget != null)
            {
                right = 0.0f;
                this.transform.LookAt(enmyTarget.transform);
                this.transform.Translate(0.0f, 0.0f, speed / 60.0f * Time.timeScale);
            }
            else
            {
                this.transform.Translate(0.0f, 0.0f, speed / 60.0f * Time.timeScale);
                this.transform.Rotate(0.0f, right * stand_RotateCoefficient / 60.0f * Time.timeScale, 0.0f);
            }             
        }
    }

    IEnumerator SimulatorNavigate()
    {
        while (startSimulator)
        {
            yield return null;

            if (enmyTarget == null)
            {
                if (nav_pointer < navigateRoute.Length)
                {
                    this.transform.LookAt(navigateRoute[nav_pointer]);
                    this.transform.Translate(0.0f, 0.0f, speed / 60.0f * Time.timeScale);         

                    frameCount += (int)(1 * Time.timeScale);
                                    
                    if(frameCount >= 3)
                    {
                        nav_pointer = nav_pointer+ (frameCount/3);
                        frameCount %= 3;
                    }
                }
                else
                    startSimulator = false;
            }
            else
            {
                this.transform.LookAt(enmyTarget.transform);
                this.transform.Translate(0.0f, 0.0f, speed / 60.0f * Time.timeScale);
            }
        }
    }

    IEnumerator PathRecord()
    {
        while (startSimulator)
        {
            if (path_Gone.GetPosition(0) == path_Gone.GetPosition(1))
                path_Gone.SetPosition(1, this.transform.position);
            else
            {
                path_Gone.positionCount++;
                path_Gone.SetPosition(path_Gone.positionCount - 1, this.transform.position);
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator RFWork()
    {
        if ((startSimulator) && (work_RF))
        {
            model_RF.SetActive(true);
            
            yield return new WaitForSeconds(0.05f);
            StartCoroutine(RFRest());
        }
    }
    IEnumerator RFRest()
    {
        if (startSimulator)
        {
            model_RF.SetActive(false);
            yield return new WaitForSeconds(4.0f);
            StartCoroutine(RFWork());
        }
    }

    public void RF_WORK()
    {
        work_RF = true;
        StartCoroutine(RFWork());
    }

    public void SettingAvoidPath(Vector3 ship_pos,float range)
    {
        Vector2 self_2D_pos = new Vector2(this.transform.position.x, this.transform.position.z);
        Vector2 tar_2D_pos = new Vector2(ship_pos.x, ship_pos.z);
        Vector2 self_f = new Vector2(this.transform.forward.x, this.transform.forward.z);

        float avoid_R = range + 2000;
        float self_r = stand_HalfLength;

        if (CheckAvoidNeed(tar_2D_pos, range))
        {
            Vector2 vec_ST = new Vector2(tar_2D_pos.x - self_2D_pos.x, tar_2D_pos.y - self_2D_pos.y);

            var f = self_f.x * vec_ST.y - vec_ST.x * self_f.y;

            Vector2 vec_n = new Vector2();

            if (f > 0) //右轉
                vec_n = new Vector2(self_f.y, -self_f.x);
            else       //左轉
                vec_n = new Vector2(-self_f.y, self_f.x);

            var tempC = new Vector2(self_2D_pos.x + vec_n.x * self_r, self_2D_pos.y + vec_n.y * self_r);

            //偏移後直線方程式 x = a+bt,y=c+dt
            var a = tempC.x;
            var b = self_f.x;
            var c = tempC.y;
            var d = self_f.y;

            //替代用參數
            var e = a - tar_2D_pos.x;
            var g = c - tar_2D_pos.y;

            //主參數整理
            var A = b * b + d * d;
            var B = 2 * b * e + 2 * d * g;
            var C = e * e + g * g - (self_r + avoid_R) * (self_r + avoid_R);

            //一元二次方程式公式
            var t1 = (-1 * B + Mathf.Sqrt(B * B - 4 * A * C)) / 2 * A;
            var t2 = (-1 * B - Mathf.Sqrt(B * B - 4 * A * C)) / 2 * A;

            //兩相切圓圓心座標
            Vector2 C_far = new Vector2(a + b * t1, c + d * t1);
            Vector2 C_close = new Vector2(a + b * t2, c + d * t2);
            Debug.Log("C_F:" + C_far);
            Debug.Log("C_C:" + C_close);


            //圓心至目標向量
            var vec_CfT = tar_2D_pos - C_far;
            var vec_CcT = tar_2D_pos - C_close;

            //開始座標 (開始繞小圈外轉時機點)
            var P1 = new Vector2(C_close.x - vec_n.x * self_r, C_close.y - vec_n.y * self_r);

            //切點座標 (停止繞小圈，改繞大圈內轉時機點)
            var P2 = new Vector2(C_close.x + vec_CcT.x * (self_r / (self_r + avoid_R)), C_close.y + vec_CcT.y * (self_r / (self_r + avoid_R)));

            //切點座標 (停止繞大圈，改繞小圈外轉時機點)
            var P3 = new Vector2(C_far.x + vec_CfT.x * (self_r / (self_r + avoid_R)), C_far.y + vec_CfT.y * (self_r / (self_r + avoid_R)));

            //結束座標 (結束繞小圈時機點)
            var P4 = new Vector2(C_far.x - vec_n.x * self_r, C_far.y - vec_n.y * self_r);

            var point1 = GameObject.Instantiate(pathGiver, new Vector3(P1.x, 10, P1.y), new Quaternion().normalized, null);
            point1.name = "P1";
            /*
            var point2 = GameObject.Instantiate(pathGiver, new Vector3(P2.x, 10, P2.y), new Quaternion().normalized, null);
            point2.name = "P2";
            var point3 = GameObject.Instantiate(pathGiver, new Vector3(P3.x, 10, P3.y), new Quaternion().normalized, null);
            point3.name = "P3";
            */
            var point4 = GameObject.Instantiate(pathGiver, new Vector3(P4.x, 10, P4.y), new Quaternion().normalized, null);
            point4.name = "P4";

            var v_P2T = tar_2D_pos - P2;
            var v_P3T = tar_2D_pos - P3;

            Vector2 v_P2T_n;
            Vector2 v_P3T_n;

            if (f > 0) //一開始右轉
            {
                v_P2T_n = new Vector2(v_P2T.y, -v_P2T.x);
                v_P3T_n = new Vector2(-v_P3T.y, v_P3T.x);
            }
            else //一開始左轉
            {
                v_P2T_n = new Vector2(-v_P2T.y, v_P2T.x);
                v_P3T_n = new Vector2(v_P3T.y, -v_P3T.x);
            }

            //L2直線方程式 x = a+bt,y=c+dt
            var a2 = P2.x;
            var b2 = v_P2T_n.x;
            var c2 = P2.y;
            var d2 = v_P2T_n.y;

            //L3直線方程式 x = a+bt,y=c+dt
            var a3 = P3.x;
            var b3 = v_P3T_n.x;
            var c3 = P3.y;
            var d3 = v_P3T_n.y;

            //L2與L3相交 a2+b2*t2 = a3+b3*t3,c2+d2*t2=c3+d3*t3

            //解 a2 + b2 * t2 = a3 + b3 * t3得t3 = v1 + v2 * t2
            var v1 = (a2 - a3) / b3;
            var v2 = b2 / b3;

            //解 c2 + d2 * t2 = c3 + d3 * t3得t3 = v3 + v4 * t2
            var v3 = (c2 - c3) / d3;
            var v4 = d2 / d3;

            //t3相等=> v1 + v2 * t2 =  v3 + v4 * t2
            var t_2 = (v1 - v3) / (v4 - v2);

            Vector2 Px = new Vector2(a2 + b2 * t_2, c2 + d2 * t_2);

            var vP2_Px = Px - P2;
            var vP3_Px = Px - P3;

            Vector2 P2d = new Vector2(P2.x + vP2_Px.x * (avoid_R - self_r) / avoid_R, P2.y + vP2_Px.y * (avoid_R - self_r) / avoid_R);
            Vector2 P3d = new Vector2(P3.x + vP3_Px.x * (avoid_R - self_r) / avoid_R, P3.y + vP3_Px.y * (avoid_R - self_r) / avoid_R);
            /*
            var point2d = GameObject.Instantiate(pathGiver, new Vector3(P2d.x, 10, P2d.y), new Quaternion().normalized, null);
            point2d.name = "P2'";
            var point3d = GameObject.Instantiate(pathGiver, new Vector3(P3d.x, 10, P3d.y), new Quaternion().normalized, null);
            point3d.name = "P3'";
            */

            Vector2 C_middle = new Vector2(tar_2D_pos.x + vec_n.x * (avoid_R - self_r), tar_2D_pos.y + vec_n.y * (avoid_R - self_r));

            Debug.Log("C_M:" + C_middle);

            float arg_a = Mathf.Asin(self_r / (Vector2.Distance(C_middle, C_close) / 2));

            Debug.Log("a:" + arg_a);       

            //切點座標 (停止繞小圈，改繞大圈內轉時機點)
            var P2_new = C_close - self_r * Mathf.Cos(arg_a) * vec_n + self_r * Mathf.Sin(arg_a) * self_f;
            var P2d_new = C_middle + self_r * Mathf.Cos(arg_a) * vec_n - self_r * Mathf.Sin(arg_a) * self_f;           

            var P3_new = C_far - self_r * Mathf.Cos(arg_a) * vec_n - self_r * Mathf.Sin(arg_a) * self_f;
            var P3d_new = C_middle + self_r * Mathf.Cos(arg_a) * vec_n + self_r * Mathf.Sin(arg_a) * self_f;

            var pointN2 = GameObject.Instantiate(pathGiver, new Vector3(P2_new.x, 10, P2_new.y), new Quaternion().normalized, null);
            pointN2.name = "P2_new";
            var pointN2d = GameObject.Instantiate(pathGiver, new Vector3(P2d_new.x, 10, P2d_new.y), new Quaternion().normalized, null);
            pointN2d.name = "P2'_new";

            var pointN3 = GameObject.Instantiate(pathGiver, new Vector3(P3_new.x, 10, P3_new.y), new Quaternion().normalized, null);
            pointN3.name = "P3_new";
            var pointN3d = GameObject.Instantiate(pathGiver, new Vector3(P3d_new.x, 10, P3d_new.y), new Quaternion().normalized, null);
            pointN3d.name = "P3'_new";

            //設定路徑點數值
            if (f > 0) //右轉
            {
                point1.GetComponent<PathGiver>().target_right = 38.6f;
                point1.GetComponent<PathGiver>().pathMode = PathMode.TURN;
                point1.GetComponent<PathGiver>().next = pointN2.GetComponent<PathGiver>();
                point1.GetComponent<PathGiver>().target_R = this.transform.rotation.eulerAngles.y;

                pointN2.GetComponent<PathGiver>().target_right = 0;
                pointN2.GetComponent<PathGiver>().pathMode = PathMode.FORWORD;
                pointN2.GetComponent<PathGiver>().next = pointN2d.GetComponent<PathGiver>();

                var vec_2 = pointN2d.transform.position - pointN2.transform.position;
                Debug.Log("Q:" + Quaternion.LookRotation(vec_2).eulerAngles);

                pointN2.GetComponent<PathGiver>().target_R = Quaternion.LookRotation(vec_2).eulerAngles.y;

                pointN2d.GetComponent<PathGiver>().target_right = -38.6f;
                pointN2d.GetComponent<PathGiver>().pathMode = PathMode.TURN;
                pointN2d.GetComponent<PathGiver>().next = pointN3d.GetComponent<PathGiver>();
                pointN2d.GetComponent<PathGiver>().target_R = pointN2.GetComponent<PathGiver>().target_R;

                pointN3d.GetComponent<PathGiver>().target_right = 0;
                pointN3d.GetComponent<PathGiver>().pathMode = PathMode.FORWORD;
                pointN3d.GetComponent<PathGiver>().next = pointN3.GetComponent<PathGiver>();

                var vec_3 = pointN3.transform.position - pointN3d.transform.position;
                Debug.Log("Q:" + Quaternion.LookRotation(vec_3).eulerAngles);
                pointN3d.GetComponent<PathGiver>().target_R = Quaternion.LookRotation(vec_3).eulerAngles.y;

                pointN3.GetComponent<PathGiver>().target_right = 38.6f;
                pointN3.GetComponent<PathGiver>().pathMode = PathMode.TURN;
                pointN3.GetComponent<PathGiver>().next = point4.GetComponent<PathGiver>();
                pointN3.GetComponent<PathGiver>().target_R = pointN3d.GetComponent<PathGiver>().target_R;

                point4.GetComponent<PathGiver>().target_right = 0;
                point4.GetComponent<PathGiver>().pathMode = PathMode.FORWORD;
                point4.GetComponent<PathGiver>().target_R = this.transform.rotation.eulerAngles.y;
            }
            else       //左轉
            {
                point1.GetComponent<PathGiver>().target_right = -38.6f;
                point1.GetComponent<PathGiver>().pathMode = PathMode.TURN;
                point1.GetComponent<PathGiver>().next = pointN2.GetComponent<PathGiver>();
                point1.GetComponent<PathGiver>().target_R = this.transform.rotation.eulerAngles.y;

                pointN2.GetComponent<PathGiver>().target_right = 0;
                pointN2.GetComponent<PathGiver>().pathMode = PathMode.FORWORD;
                pointN2.GetComponent<PathGiver>().next = pointN2d.GetComponent<PathGiver>();

                var vec_2 = pointN2d.transform.position - pointN2.transform.position;
                Debug.Log("Q:" + Quaternion.LookRotation(vec_2).eulerAngles);

                pointN2.GetComponent<PathGiver>().target_R = Quaternion.LookRotation(vec_2).eulerAngles.y;

                pointN2d.GetComponent<PathGiver>().target_right = 38.6f;
                pointN2d.GetComponent<PathGiver>().pathMode = PathMode.TURN;
                pointN2d.GetComponent<PathGiver>().next = pointN3d.GetComponent<PathGiver>();
                pointN2d.GetComponent<PathGiver>().target_R = pointN2.GetComponent<PathGiver>().target_R;

                pointN3d.GetComponent<PathGiver>().target_right = 0;
                pointN3d.GetComponent<PathGiver>().pathMode = PathMode.FORWORD;
                pointN3d.GetComponent<PathGiver>().next = pointN3.GetComponent<PathGiver>();

                var vec_3 = pointN3.transform.position - pointN3d.transform.position;
                Debug.Log("Q:" + Quaternion.LookRotation(vec_3).eulerAngles);
                pointN3d.GetComponent<PathGiver>().target_R = Quaternion.LookRotation(vec_3).eulerAngles.y;

                pointN3.GetComponent<PathGiver>().target_right = -38.6f;
                pointN3.GetComponent<PathGiver>().pathMode = PathMode.TURN;
                pointN3.GetComponent<PathGiver>().next = point4.GetComponent<PathGiver>();
                pointN3.GetComponent<PathGiver>().target_R = pointN3d.GetComponent<PathGiver>().target_R;

                point4.GetComponent<PathGiver>().target_right = 0;
                point4.GetComponent<PathGiver>().pathMode = PathMode.FORWORD;
                point4.GetComponent<PathGiver>().target_R = this.transform.rotation.eulerAngles.y;
            }
        }
        else
        {
            Debug.Log("Not need to avoid!");
        }
    }

    public bool CheckAvoidNeed(Vector2 ship_pos, float range)
    {
        //原直線方程式 ax+by+c = 0
        Vector2 self_2D_pos = new Vector2(this.transform.position.x, this.transform.position.z);
        Vector2 self_f = new Vector2(this.transform.forward.x, this.transform.forward.z);

        var a = self_f.y;
        var b = -1 * self_f.x;
        var c = -1 * a * self_2D_pos.x - b * self_2D_pos.y;

        var dis = Mathf.Abs(a * ship_pos.x + b * ship_pos.y + c) / Mathf.Sqrt((a * a + b * b));

        if (dis >= range)
            return false;
        else
            return true;   
    }

    private void OnGUI()
    {
        if (text_x != null)
        {
            if (Mathf.Abs(x_value) >= 1000)
            {
                text_x.text = (this.transform.transform.position.x / 1000).ToString("0.0");
                text_x_stand.text = "km";
            }
            else
            {
                text_x.text = (this.transform.transform.position.x).ToString("0.0");
                text_x_stand.text = "m";
            }
        }
        if (text_z != null)
        {
            if (Mathf.Abs(z_value) >= 1000)
            {
                text_z.text = (this.transform.transform.position.z / 1000).ToString("0.0");
                text_z_stand.text = "km";
            }
            else
            {
                text_z.text = (this.transform.transform.position.z).ToString("0.0");
                text_z_stand.text = "m";
            }
        }
        if (text_h != null)
        {
            if (Mathf.Abs(h_value) >= 1000)
            {
                text_h.text = (this.transform.transform.position.y / 1000).ToString("0.0");
                text_h_stand.text = "km";
            }
            else
            {
                text_h.text = (this.transform.transform.position.y).ToString("0.0");
                text_h_stand.text = "m";
            }
        }
        if (text_v != null)
            text_v.text = speed.ToString("0.0");
        if (text_r != null)
            text_r.text = this.transform.eulerAngles.y.ToString("0.0");
        if (imagePoint != null)
            imagePoint.transform.localEulerAngles = new Vector3(0.0f, 0.0f, -1 * this.transform.localEulerAngles.y);
    }

    // Update is called once per frame
    void Update()
    {
        if (limitFPS)
        {
            if (Application.targetFrameRate != 60)
                Application.targetFrameRate = 60;
        }
        animator.SetFloat("Up", up);
        animator.SetFloat("Right", right);
        if (work)
        {
            work = false;
            StartSimulator();
        }
        x_value = this.transform.transform.position.x;
        z_value = this.transform.transform.position.z;
        h_value = this.transform.transform.position.y;
    }

    public void ChangeR(float value)
    {
        if (!startSimulator)
            return;
        if (value > 45.0f)
            right = 45.0f;
        else if (value < -45.0f)
            right = -45.0f;
        else
            right = value;
    }
}
