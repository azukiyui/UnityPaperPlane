using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PaperPlane
{
    public class PaperPlane : MonoBehaviour
    {
        [SerializeField] protected Color color = Color.white;
        [SerializeField] protected float timeScale = 1;
        [Serializable]
        public class Plane
        {
            [SerializeField] protected float initV = 3.7f;
            [SerializeField] protected float initY = 0;
            [SerializeField] protected float initH = 2;

            [SerializeField] protected float m = 0.003f;//weight kg
            [SerializeField] protected float wingsSpan = 0.12f; //m
            //[SerializeField] protected float planeLength = 0.28f; //m
            [SerializeField] protected float S = 0.017f;//Wing Area m2
            [SerializeField] protected float ar = 0.86f; //wingsSpan*wingsSpan/wingArea

            [SerializeField] protected float g = 9.807f;//gravity m/s2
            [SerializeField] protected float p = 1.225f;//air density kg/m3

            //Runtime variables
            [SerializeField] protected float attackAngle = 9.3f * Mathf.Deg2Rad;//alpha
            [SerializeField] protected float CLx = 0;
            [SerializeField] protected float CL = 0;
            [SerializeField] protected float CD = 0;

            [SerializeField] protected float v = 0;//air speed
            [SerializeField] protected float y = 0;//flight path angle
            [SerializeField] protected float h = 0;//height
            [SerializeField] protected float r = 0;//range

            [SerializeField] protected float t = 0;//time

            protected delegate T DyDx<T>(T y, float x);

            public Vector3 Pos => new Vector3(this.r, this.h, 0);
            public Quaternion Rot=> Quaternion.Euler(0, 0, this.y * Mathf.Rad2Deg);

            public void PrepareParameters(Plane other = null)
            {
                if (other != null)
                {
                    this.initV = other.initV;
                    this.initY = other.initY;
                    this.initH = other.initH;

                    this.m = other.m;
                    this.wingsSpan = other.wingsSpan;
                    this.S = other.S;
                    this.ar = other.ar;
                    this.g = other.g;
                    this.p = other.p;

                    this.attackAngle = other.attackAngle;
                }

                this.ar = wingsSpan * wingsSpan / S;
                var ar2 = (ar / 2) * (ar / 2);
                this.CLx = (Mathf.PI * ar) / (1 + Mathf.Sqrt(1 + ar2));
                this.CL = this.CLx * this.attackAngle;

                var e = 1 / (Mathf.PI * 0.9f * ar);
                this.CD = 0.02f + e * CL * CL;

                this.v = this.initV;
                this.y = this.initY * Mathf.Deg2Rad;

                this.h = this.initH;
                this.r = 0;
                this.t = 0;
            }

            public void Process(float dt)
            {
                var nv = this.Step(DvDt, v, t, dt);
                var ny = this.Step(DyDt, y, t, dt);
                var nh = this.Step(DhDt, h, t, dt);
                var nr = this.Step(DrDt, r, t, dt);
                t += dt;

                v = nv;
                y = ny;
                h = nh;
                r = nr;
            }
            protected float DrDt(float h, float t)
            {
                return v * Mathf.Cos(y);
            }
            protected float DhDt(float h, float t)
            {
                return v * Mathf.Sin(y);
            }
            protected float DvDt(float v, float t)
            {
                return -CD * (0.5f * p * v * v) * S / m - g * Mathf.Sin(y);
            }
            protected float DyDt(float y, float t)
            {
                return (CL * (0.5f * p * v * v) * S / m - g * Mathf.Cos(y)) / v;
            }

            protected float Step(DyDx<float> func, float yn, float xn, float h)
            {
                var k1 = h * func(yn, xn);
                var k2 = h * func(yn + 0.5f * k1, xn + 0.5f * h);
                yn = yn + k2;

                return yn;
            }
        }


        [SerializeField] protected Plane plane = new Plane();
        [SerializeField] protected Plane planeCurve = new Plane();
        protected void Start()
        {
            this.plane.PrepareParameters();
        }

        protected void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) this.plane.PrepareParameters();

            for (int i = 0; i < 8; ++i)
            {
                this.plane.Process(Time.deltaTime/8 * this.timeScale);
            }

            this.transform.position = this.plane.Pos;
            this.transform.rotation = this.plane.Rot;
        }

        protected void OnDrawGizmos()
        {
            Gizmos.color = this.color;
            this.plane.PrepareParameters();
            this.planeCurve.PrepareParameters(this.plane);

            Gizmos.DrawRay(this.plane.Pos, this.plane.Rot * Vector3.right);

            for (var i = 0; i < 5000; ++i)
            {
                var prev = this.planeCurve.Pos;
                this.planeCurve.Process(0.001f);
                var pos = this.planeCurve.Pos;

                Gizmos.DrawLine(prev, pos);
            }
        }
    }
}