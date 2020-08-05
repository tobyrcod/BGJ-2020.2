using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Rope : MonoBehaviour
{
    /*Euler Integration
     * NewPos = OldPos + v * t
     * 
     *Verlet Integration, Assumes the object will continue moving in uniform motion based on its previous position
     * NewPos = CurrentPos + (CurrentPos - OldPos)
    */

    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();
    
    [SerializeField] private int accuracyLevel = 50;
    [SerializeField] private int numberOfSegments = 35;
    [SerializeField] private float ropeSegLen = 0.25f;
    [SerializeField] private Transform ropeStartTrasform;
    [SerializeField] private Transform ropeEndTransform;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Vector2 gravity = new Vector2(0f, -1f);

    private void Start() {
        this.lineRenderer = this.GetComponent<LineRenderer>();

        Vector2 ropeStartPos = ropeStartTrasform.position;
        for (int i = 0; i < numberOfSegments; i++) {
            this.ropeSegments.Add(new RopeSegment(ropeStartPos));
            ropeStartPos.y -= ropeSegLen;
        }
    }

    private void FixedUpdate() {
        Simulate();
        DrawRope();
    }

    private void Simulate() {

        //SIMULATION
        for (int i = 0; i < numberOfSegments; i++) {
            RopeSegment segment = this.ropeSegments[i];
            Vector2 velocity = segment.posNow - segment.posOld;
            segment.posOld = segment.posNow;
            segment.posNow += velocity;
            segment.posNow += gravity * Time.deltaTime;
            ropeSegments[i] = segment;
        }

        //CONSTRAINTS
        for (int i = 0; i < accuracyLevel; i++) {
            ApplyConstraints();
        }
    }

    private void ApplyConstraints() {

        //Make sure the first segment is in the correct position
        RopeSegment firstSegment = ropeSegments[0];
        firstSegment.posNow = ropeStartTrasform.position;
        ropeSegments[0] = firstSegment;

        //Make sure the last segment is in the correct position
        RopeSegment lastSegment = ropeSegments[numberOfSegments - 1];
        lastSegment.posNow = ropeEndTransform.position;
        ropeSegments[numberOfSegments - 1] = lastSegment;

        //Two segments of rope have to maintain a set distance apart.
        for (int i = 0; i < numberOfSegments - 1; i++) {
            RopeSegment currentSeg = ropeSegments[i];
            RopeSegment nextSeg = ropeSegments[i + 1];

            //Get the distance the two segments are apart
            float dist = (currentSeg.posNow - nextSeg.posNow).magnitude;

            //Calculates the magnitude of the size error between the two segments
            float error = Mathf.Abs(dist - ropeSegLen);

            if (error != 0) {
                //If the segments arent in the correct positions

                //The direction between the segments;
                Vector2 changeDir = Vector2.zero;

                if (dist > ropeSegLen) {
                    //If the distance between the segments is too big
                    changeDir = (currentSeg.posNow - nextSeg.posNow).normalized;
                }
                else if (dist < ropeSegLen) {
                    //If the distance between the segments is too small
                    changeDir = (nextSeg.posNow - currentSeg.posNow).normalized;
                }

                Vector2 changeAmount = changeDir * error;
                if (i != 0) {
                    currentSeg.posNow -= changeAmount * 0.5f;
                    ropeSegments[i] = currentSeg;
                }

                nextSeg.posNow += changeAmount;
                ropeSegments[i + 1] = nextSeg;
            }
        }
    }

    private void DrawRope() {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        Vector3[] ropePositions = new Vector3[numberOfSegments];
        for (int i = 0; i < numberOfSegments; i++) {
            ropePositions[i] = ropeSegments[i].posNow;
        }

        lineRenderer.positionCount = ropePositions.Length;
        lineRenderer.SetPositions(ropePositions);
    }

    public struct RopeSegment {
        public Vector2 posNow;
        public Vector2 posOld;

        public RopeSegment(Vector2 pos) {
            this.posNow = pos;
            this.posOld = pos;
        }
    }
}
 