using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NLegIKSolver : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform[] legEnds; // Objects to be moved
    [SerializeField] Transform[] Bodies; // To not step under objets
    [SerializeField] AudioSource legStepSound;
    [Header("Movement")]
    [SerializeField] bool stickToMovingObjects = false;
    [SerializeField] bool _turnFeets; // enables feet rotation
    [SerializeField] float speed = 1.0f; // Speed of interpolation
    [SerializeField] float stepHeight = 1.0f; // how high will step
    [SerializeField] float stepZoneRadius = 4.0f; // Radius of a zone where the leg stays
    [SerializeField] float stepAheadLength = 4.0f; // If 0 the leg will return in the start position every step
    [SerializeField] float noStepZoneRadius = 4.0f; // Radius of a zone under Bodies where leg can't step
    [SerializeField] float legsAttachHeight = 10f;
    [SerializeField] float legsRayTolerance = 2f; // to adjust height of ray projection

    private Vector3[] newPosition, oldPosition, tempPosition;
    private Vector3[] _stepZonesPos; // To store local coords of zones
    private Quaternion[] _stepZonesRot; // To store local coords of zones
    private Vector3[] _stepPos; // to store global position of every leg
    private Vector3[] _noStepZonesPos; // to create zones for leg to step out
    private float[] _lerp; // array of values to interpolate
    private bool[] isLegStands;
    float baseSpeed, baseStepAhead;
    Vector3 oldPosBody;
    private bool isWorking = true;

    private Transform[] _underObjectOldTransform;
    private Vector3[] _oldTransformOldPos;
    void Awake()
    {
        int arrayLength = legEnds.Length;
        _lerp = new float[arrayLength];
        newPosition = new Vector3[arrayLength];
        tempPosition = new Vector3[arrayLength];
        oldPosition = new Vector3[arrayLength];
        _stepZonesPos = new Vector3[arrayLength];
        _stepZonesRot = new Quaternion[arrayLength];
        _stepPos = new Vector3[arrayLength];
        isLegStands = new bool[arrayLength];
        _oldTransformOldPos = new Vector3[arrayLength];
        _underObjectOldTransform = new Transform[arrayLength];

        for (int i = 0; i < arrayLength; i++)
        {
            _lerp[i] = 1f;
            _stepPos[i] = legEnds[i].position;
            _stepZonesPos[i] = legEnds[i].localPosition;
            _stepZonesRot[i] = legEnds[i].localRotation;
            newPosition[i] = legEnds[i].position;
            tempPosition[i] = legEnds[i].position;
            oldPosition[i] = legEnds[i].position;
            isLegStands[i] = true;
        }

        int arrayLengthNoStep = Bodies.Length;
        _noStepZonesPos = new Vector3[arrayLengthNoStep];

        for (int i = 0; i < arrayLengthNoStep; i++)
        {
            _noStepZonesPos[i] = Bodies[i].localPosition;
        }

            if (stepAheadLength > stepZoneRadius)
            stepAheadLength = stepZoneRadius * 0.95f;

        baseSpeed = speed;
        baseStepAhead = stepAheadLength;
        oldPosBody = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (isWorking)
        {
            Vector3 moveDirection = transform.position - oldPosBody;
            float addSpeed = (Mathf.Abs(moveDirection.magnitude)) * 25f;
            speed = baseSpeed + addSpeed;
            stepAheadLength = baseStepAhead + addSpeed / 4.0f;
            for (int i = 0; i < _stepZonesPos.Length; i++)
            {
                Vector3 zoneWorldPos = transform.TransformPoint(_stepZonesPos[i]);
                Ray ray = new Ray(_stepPos[i] + legsRayTolerance * transform.up, -transform.up);
                int indexPlus = i + 1;
                int indexMinus = i - 1;

                if (indexPlus == _stepZonesPos.Length)
                    indexPlus = 0;

                if (indexMinus < 0)
                    indexMinus = _stepZonesPos.Length - 1;

                if (Physics.Raycast(ray, out RaycastHit rayHit, legsRayTolerance + legsAttachHeight, terrainLayer.value) && (_stepPos[i] - zoneWorldPos).magnitude < 3f * stepZoneRadius)
                {
                    // Zones are stored locally
                    Vector3 relativeRayPos = transform.InverseTransformPoint(rayHit.point); // Converting to compare with local zones
                    isLegStands[i] = true;

                    if ((Vector3.Distance(zoneWorldPos, rayHit.point) > stepZoneRadius || Contains(relativeRayPos, _noStepZonesPos, noStepZoneRadius)) &&
                        _lerp[i] >= 1 && _lerp[indexPlus] >= 1 && _lerp[indexMinus] >= 1)
                    {
                        oldPosition[i] = _stepPos[i];
                        Vector3 newStepPos = zoneWorldPos + (zoneWorldPos - rayHit.point).normalized * stepAheadLength;
                        _lerp[i] = 0.0f;

                        if (Contains(newStepPos, _noStepZonesPos, noStepZoneRadius))
                            newPosition[i] = _stepPos[i] = zoneWorldPos;
                        else
                            newPosition[i] = newStepPos;
                    }
                    else if (_lerp[i] >= 1)
                        legEnds[i].position = Vector3.Lerp(legEnds[i].position, rayHit.point, 0.5f);

                    if (_turnFeets)
                        legEnds[i].up = rayHit.normal;

                    if (stickToMovingObjects)
                    {
                        if (_underObjectOldTransform[i] == rayHit.transform)
                        {
                            _stepPos[i] += rayHit.transform.position - _oldTransformOldPos[i];
                        }
                        _underObjectOldTransform[i] = rayHit.transform;
                        _oldTransformOldPos[i] = _underObjectOldTransform[i].position;
                    }
                }
                else
                {
                    legEnds[i].position = _stepPos[i] = Vector3.Lerp(legEnds[i].position, zoneWorldPos, 0.01f);
                    isLegStands[i] = false;
                }

                if (_lerp[i] < 1) // Interpolation between two positions
                {
                    tempPosition[i] = Vector3.Lerp(oldPosition[i], newPosition[i], _lerp[i]);
                    tempPosition[i] -= Vector3.Project(tempPosition[i] - rayHit.point, rayHit.normal); // add offset to objects by normal
                    tempPosition[i] += stepHeight * Mathf.Sin(_lerp[i] * Mathf.PI) * transform.up; // add offset
                    _lerp[i] += Time.deltaTime * speed;
                    legEnds[i].position = _stepPos[i] = tempPosition[i];

                    if (_lerp[i] >= 1 && legStepSound != null)
                        legStepSound.Play();
                }
            }
            oldPosBody = transform.position;
        }
        
    }
    bool Contains(Vector3 point, Vector3[] zone, float radius)
    {
        bool condition = true;
        for (int i = 0; i < zone.Length; i++)
        {
            point.y = zone[i].y;
            if (Vector3.Distance(zone[i], point) > radius)
            {
                condition = false;
                break;
            }
        }
        return condition;
    }
    public bool IsLegStanding(int index)
    {
        return isLegStands[index];
    }
    public int NumberOfLegs()
    {
        return legEnds.Length;
    }
    public void DisableScript()
    {
        isWorking = false;
    }
    public void OnDisable()
    {
        isWorking = true;
    }
}


