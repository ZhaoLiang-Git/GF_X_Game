using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Solo.MOST_IN_ONE
{
    
    public class Most_Spawn : MonoBehaviour
    {
        /// <summary>
        /// Most Spawn system...
        /// please read MOST documentation and API and watch the MOST_Spawn explaining video (Old verison but will help)
        /// </summary>

        public enum SpawnShapeType { Circle, Square }
        public enum CyclesType { LoopForever, LimittedCycles }
        public enum SpawnType { DividedByCycleTime, SingleSwarm }
        public enum SelectObjectType { Random, Loop }
        public enum RotationSettings { NoRotation, LookAtInsideShape, LookAtOutSideShape, NoTargetPoint_Random }

        [HideInInspector] public bool Enable { get; private set; } // A Quick readonly Check if this behavior is enabled or not
        [HideInInspector] public List<GameObject> ActiveObjectsInScene = new(); // Contains all Active spawn objects in the scene
        [HideInInspector] public List<Vector3> CurrentSpawnPoints = new(); // Updated for each spawn call, will be one element if ""SpawnType = DividedByCycleTime"" and List if ""SpawnType = SingleSwarm""
        [HideInInspector] public List<Vector3> CurrentSpawnAngles = new(); // Updated for each spawn call, will be one element if ""SpawnType = DividedByCycleTime"" and List if ""SpawnType = SingleSwarm""
        [HideInInspector] public List<GameObject> CurrentSpawnObjects = new(); // Updated for each spawn call, will be one element if ""SpawnType = DividedByCycleTime"" and List if ""SpawnType = SingleSwarm""

        [BigHeader("Main Settings")] [Space]// Main Settings Sections
        [Tooltip("Enable on Start, or you can call EnableState(true) to enable this behavior")]
        public bool EnableOnStart;

        [Tooltip("List of object to spawn, you can keep it empty and the system will " +
            "spawn only transforms stored in public list CurrentSpawnPoints and CurrentSpawnAngles")]
        public GameObject[] SpawnObjectList;

        [Tooltip("Spawn Object Select way")]
        public SelectObjectType SelectType;

        [Tooltip("(Optional) the Parent in scene to all spawn objects")]
        public GameObject AsChildTo;
        // ____________ End Main Settings Section ____________

        [BigHeader("Spawn Position Settings")] [Space] // Spawn Position Section
        [Tooltip("Spawn Area Shape, Enable Debug to draw it, the Shape Contains two parts\n" +
            " Inner and Outer, the Spawn area is between these two parts")]
        public SpawnShapeType SpawnShape;

        [Tooltip("the ignored points inside spawn area if it contains these gameObject layers")]
        public LayerMask IgnoredPositionsLayers;

        [Tooltip("Inner Shape Scale")]
        [Min(0)] public float InnerScale = 0;

        [Tooltip("Outer Shape Scale")]
        [Min(0)] public float OuterScale = 16;

        [Tooltip("Y Axis Scale (The hight of the shape) for 3D Spawn area")]
        [Min(0)] public float Hight = 0;
        // ____________ End Spawn Position Section ____________

        [BigHeader("Snap Position")] [Space] // Snap Point Section
        [Tooltip("Spawn point will be snapped to perfect points starting from the center spawn area")]
        public bool EnableSnap;

        [Tooltip("The Grid Size of the snap starting from the center spawn area")]
        public Vector3 GridSize = new(1, 0, 1);
        // ____________ End Snap Point Section ____________//

        [BigHeader("Spawn Cycle Settings")] [Space] // Spawn Cycle Control Section
        [Tooltip("Loop for Ever > the spawn system will loop forever until it stops manually unsing EnableSpawn(false)\n" +
            "Limited Cycle > the spawn system will stops  after amount of cycels (NumberOfCycles) or manually as well")]
        public CyclesType RepeatSystem;

        [Tooltip("Single swarm will make spawn system spawn all SpawnRatePerCycle at the beginning of the cycle")]
        public SpawnType AmountSystem;

        [Tooltip("Number of spawn object foreach cycle")]
        [Min(0.1f)] public int SpawnRatePerCycle = 4;

        [Tooltip("Time of the Spawn cycle")]
        [Min(0.1f)] public float CycleTime = 3;

        [Tooltip("Time Between End of the cycle and starting of the new cycle")]
        [Min(0)] public float RestTimeBetweenCycles;

        [Tooltip("Number of cycles on this spawn system")]
        public int NumberOfCycles;

        [Tooltip("Time before starting the first cycle (Starting of the spawn system)")]
        public float FirstCycleDelay;
        // ____________ End Spawn Cycle Control Section ____________

        [BigHeader("Spawn Rate Accelerator")] [Space]
        [Tooltip("If Enable, SpawnRatePerCycle will move toward TargetSpawnRate\nYou can enable/disable it in run time")]
        public bool EnableRateAccelerator;

        [Tooltip("The new spawn rate (can be less or more than SpawnRatePerCycle)")]
        [Min(0)] public int TargetSpawnRate;

        [Tooltip("This value will be added to current spawn rate per UpdateTime")]
        [Min(1f)] public int AddedAmount;

        [Tooltip("Time between each rate update")]
        [Min(0.01f)] public float UpdateTime;
        // ____________ End Spawn Rate Accelerator ____________

        [BigHeader("Object Rotation Settings")] [Space] // Object Rotation Control Section
        [Tooltip("No Rotation: Spawn Prefab Rotation \nLookInside: look at the center of the shape + The Defined Random Range \n" +
            "LookOutSide: look at the opposite of the center + The Defined Random Range \nNo Target: Only dependeing on The Defined RandomRange")]
        public RotationSettings TargetRotation;

        [Tooltip("Spread the rotation range of the TargetRotation to spawn a perfect system for random rotaion")]
        [Range(-360, 360)] public float RandomRange_Minimum;
        [Range(-360, 360)] public float RandomRange_Maximum;
        // ____________ End Object Rotation Control Section ____________

        [BigHeader("Number Of Objects Control")] [Space] // Number Of Objects Control Section
        [Tooltip("The amount of spawn Active objects will be limited in the scene (Only Active)" +
            "...if reached the max amount the system will hold until objects destroyed")]
        public bool LimitActiveObjectsInScene;

        [Tooltip("The Max amount of spawn Active objects in the scene")]
        public int MaxNumberInScene;

        [Tooltip("curent number of the active object in the scene")]
        [ReadOnly] public int CurrentActiveObjects;

        [Tooltip("number of spawned object using this spawn system")]
        [ReadOnly] public int TotalSpawnedObjects;
        // ____________ End Number Of Objects Section ____________

        [Space]
        [BigHeader("Events Section")] [Space]
        [Tooltip("These Events will be called At the Start of a new cycle")]
        public UnityEvent OnCycleStart = new(); // Events when Cycle Starts

        [Tooltip("These Events will be called At the End of the current cycle")]
        public UnityEvent OnCycleEnd = new(); // Events when Cycle Ends

        [Tooltip("These Events will be called at each new spawn (new object Instantiated) and send the spawn point")]
        public UnityEvent<Vector3> OnSpawnCalledEvent = new();// Events when a New Object Spawned // Called for each Spawn Event
                                                              // ____________ End Spawn Position Section ____________
        int _cycleCounter, _falseTryCounter, _spawnCounter, _selector;
        bool _activate;

        void OnValidate() // For inspector debugger and attributs control
        {
            // if all gird axis sizes = 0 the snapping will be breaked, you can change min value if 0.1f not fit...
            GridSize.x = Mathf.Max((float)System.Math.Round(GridSize.x, 1), 0.1f);
            GridSize.y = Mathf.Max((float)System.Math.Round(GridSize.y, 1), 0);
            GridSize.z = Mathf.Max((float)System.Math.Round(GridSize.z, 1), 0.1f);

            OuterScale = Mathf.Max(OuterScale, InnerScale); // to make sure min not exeeded max

            RandomRange_Maximum = Mathf.Max(RandomRange_Maximum, RandomRange_Minimum); // to make sure min not exeeded max

            if (RepeatSystem == CyclesType.LoopForever) NumberOfCycles = -1; // Number of cycles is infinity if CyclesType = loop forever
            else NumberOfCycles = Mathf.Max(NumberOfCycles, 1);
        }

        void Start()
        {
            EnableState(EnableOnStart); // apply EnableOnStart
        }

        #region Cycle Handler
        IEnumerator MainSpawnCycle()
        {
            OnCycleStart.Invoke(); // Start Cycle and call OnCycleStart Events

            // if this is the fist cycle of this system use the first cycle delay
            if (!_activate) yield return new WaitForSeconds(FirstCycleDelay);
            else if (AmountSystem == SpawnType.SingleSwarm) yield return new WaitForSeconds(CycleTime); // Single swarm delay

            ActiveObjectsInScene.RemoveAll(item => item == null); // remove all null and destroyed objects

            CurrentSpawnObjects.Clear(); // reset all current cycle data store list for new cycle
            CurrentSpawnPoints.Clear();  // positions
            CurrentSpawnAngles.Clear();  // rotations

            while (_spawnCounter < SpawnRatePerCycle)
            {
                _spawnCounter++; _falseTryCounter = 0;
                // DividedByCycleTime Delay - note that devided by cycle time delay is inside loop and single swarm not :)
                if (AmountSystem == SpawnType.DividedByCycleTime && _activate) yield return new WaitForSeconds(CycleTime / SpawnRatePerCycle);

                _activate = true; // first cycle started

                // if you set limitation to active spawned object in the scene
                if (LimitActiveObjectsInScene && ActiveObjectsInScene.Count >= MaxNumberInScene)
                {
                    StartCoroutine(MainSpawnCycle()); // start a new cycle for delay and check again after delay 
                    yield break; // and break this cycle
                }

                // _selector is the pointer for SpawnObjectList 
                if (SelectType == SelectObjectType.Random) _selector = Random.Range(0, SpawnObjectList.Length); // Random selection
                else _selector = Mathf.Min(_selector + 1, SpawnObjectList.Length); // loop selection
                if (_selector == SpawnObjectList.Length) _selector = 0; // if _selector loop reach the length of the list start again from begaining

                Vector3 position = PositionGenerator(); // generate new spawn position
                if (_falseTryCounter > 200) continue; // if the system tries 200 times to find a spawn point and faild break this loop... Check PositionGenerator()

                Vector3 rotation = RotationGenerator(position); // generate new rotation... check RotationGenerator()

                CurrentSpawnPoints.Add(position); // add the new position to the new points list
                CurrentSpawnAngles.Add(rotation); // add the new rotation to the new Angles list

                if (SpawnObjectList.Length > 0) // check if there is an object to spawn and add it to SpawnStore List
                {
                    // Spawn new object and add it to ActiveObjectsInScene list
                    ActiveObjectsInScene.Add(Instantiate(SpawnObjectList[_selector], position,
                        Quaternion.Euler(rotation), AsChildTo ? AsChildTo.transform : null));
                    CurrentSpawnObjects.Add(ActiveObjectsInScene[^1]); // and CurrentSpawnObjects list

                    TotalSpawnedObjects++;
                    CurrentActiveObjects = ActiveObjectsInScene.Count;
                }
                OnSpawnCalledEvent.Invoke(position); // call OnSpawnCalled Event
            } // Spawn loop finished // Current cycle finished

            OnCycleEnd.Invoke(); // now call OnCycleEnd event

            _cycleCounter++; _spawnCounter = 0;
            if (RestTimeBetweenCycles > 0) yield return new WaitForSeconds(RestTimeBetweenCycles); // enable rest time between cycles if there

            // Check if the system CyclesType is limited cycles // if _cycleCounter exeeded max NumberOfCycles, disable the spawn system
            if (RepeatSystem == CyclesType.LimittedCycles && _cycleCounter >= NumberOfCycles) EnableState(false);
            else StartCoroutine(MainSpawnCycle()); // else start new cycle
        }

        IEnumerator CustomSpawnCycle(int Amount, float DelayBetweenEachSpawn)
        {
            for (int i = 0; i < Amount; i++)
            {
                yield return new WaitForSeconds(DelayBetweenEachSpawn); // Delay

                // _selector is the pointer for SpawnObjectList 
                if (SelectType == SelectObjectType.Random) _selector = Random.Range(0, SpawnObjectList.Length); // Random selection
                else _selector = Mathf.Min(_selector + 1, SpawnObjectList.Length); // loop selection
                if (_selector == SpawnObjectList.Length) _selector = 0; // if _selector loop reach the length of the list start again from begaining

                Vector3 position = PositionGenerator(); // generate new spawn position
                if (_falseTryCounter > 200) continue; // if the system tries 200 times to find a spawn point and faild break this loop... Check PositionGenerator()

                Vector3 rotation = RotationGenerator(position); // generate new rotation... check RotationGenerator()

                if (SpawnObjectList.Length > 0) // check if there is an object to spawn and add it to SpawnStore List
                {
                    // Spawn new object and add it to ActiveObjectsInScene list
                    ActiveObjectsInScene.Add(Instantiate(SpawnObjectList[_selector], position,
                        Quaternion.Euler(rotation), AsChildTo ? AsChildTo.transform : null));
                    // note that custom spawn objects not added to current spawn obj list bc these spawns not from main spawn current cycle
                }
                else Debug.LogWarning("There are no objects to spawn");
            }
        }

        IEnumerator AccelerationControl()
        {
            yield return new WaitForSeconds(UpdateTime);
            if (EnableRateAccelerator) SpawnRatePerCycle = (int)Mathf.MoveTowards(SpawnRatePerCycle, TargetSpawnRate, AddedAmount);
            StartCoroutine(AccelerationControl());
        }
        #endregion

        #region Transform Generator Methods

        Vector3 PositionGenerator() // main position generator
        {
            Vector3 spawnPos = Vector3.zero; // the vector that will be modified and return
            if (SpawnShape == SpawnShapeType.Square) // generate random point in Square
            {
                // generate a random float between inner and outer shape scale // positive or negative
                float selectedLine = Random.Range(InnerScale / 2, OuterScale / 2) * (Random.value < 0.5f ? -1 : 1);
                bool isXAxis = Random.value < 0.5f; // if this float will be in x axis or z

                // set this float in one axis and generate the other axis
                spawnPos.x = isXAxis ? selectedLine : Random.Range(-OuterScale / 2, OuterScale / 2);
                spawnPos.z = isXAxis ? Random.Range(-OuterScale / 2, OuterScale / 2) : selectedLine;
            }
            else if (SpawnShape == SpawnShapeType.Circle) // generate random point in Circle
            {
                float angleReference = Random.Range(0f, Mathf.PI * 2);
                float radius = Mathf.Sqrt(Random.Range(InnerScale * InnerScale / 4, OuterScale * OuterScale / 4));
                spawnPos = new Vector3(Mathf.Cos(angleReference), 0, Mathf.Sin(angleReference)) * radius;
            }
            if (Hight > 0) spawnPos.y = Random.Range(0, Hight); // now generate a random hight if the shape is 3d
            if (EnableSnap) spawnPos = SnapVector(spawnPos); // snap the generated vector if snap enabled
            spawnPos += transform.position; // set the point from local transform to global transform

            Ray ray = new(spawnPos + Vector3.up * 3, Vector3.down); // create a ray on this new point
            if (Physics.Raycast(ray, 3, IgnoredPositionsLayers)) // check if the point contains any obstiacls (IgnoredPositionsLayers)
            {
                _falseTryCounter++; // count up _falseTryCounter
                if (_falseTryCounter > 200) // if number of tries exeeded 200
                {
                    Debug.LogWarning("The Spawn Generator Can't Find A Point (Most Or All Spawn area filled by IgnoredPoitionsLayers Objects)");
                    Debug.LogWarning("Error: Point Generator Breaked! return transform.position.....)");
                    return transform.position;
                }
                return PositionGenerator(); // retry generating anther point
            }
            _falseTryCounter = 0; // reset false try counter if succeded
            return spawnPos; // spawn point generated
        }

        Vector3 RotationGenerator(Vector3 spawnPosition) // generate a Y axis rotation in euler angle
        {
            Vector3 direction; // store the start direction point
            if (TargetRotation == RotationSettings.NoRotation) // if no rotation return the prefab transform.rotation in euler of return Vector3.zero if there is no prefab
                return _selector > 0 && SpawnObjectList.Length > 0 ? SpawnObjectList[_selector].transform.eulerAngles : Vector3.zero;

            else if (TargetRotation == RotationSettings.LookAtInsideShape) // if the start calcuation point is center of spawn shape
                direction = (transform.position - spawnPosition).normalized;

            else if (TargetRotation == RotationSettings.LookAtOutSideShape) // if the start calcuation point is oposite of center of spawn shape
                direction = (spawnPosition - transform.position).normalized;

            else // TargetRotation == RotationSettings.NoTargetPoint // if there is no start calcuation point (full random rotation)
                direction = Vector3.zero;

            return Vector3.up * (Mathf.Atan2(direction.x, direction.z) * 180 / Mathf.PI + Random.Range(RandomRange_Minimum, RandomRange_Maximum));
        }

        Vector3 SnapVector(Vector3 pos) // universal vector snap method
        {
            Vector3 snap = new(Mathf.Abs(pos.x), 0, Mathf.Abs(pos.z)); // remove the sign(Abs) for prefect snapping
            snap.x = Mathf.Round(snap.x / GridSize.x) * GridSize.x; // snap x axis 
            snap.z = Mathf.Round(snap.z / GridSize.z) * GridSize.z; // snap z axis 
            if (SpawnShape == SpawnShapeType.Square)
            {
                if (snap.x < InnerScale / 2 && snap.z < InnerScale / 2) // check if the snapping make the point out of shape area
                    AdjustPosition(ref snap, GridSize.x, GridSize.z); // check AdjustPosition()

            }
            else // SpawnShape == SpawnShapeType.Circle
            {
                if (snap.magnitude > OuterScale / 2) // check if the snapping make the point out of shape area
                    AdjustPosition(ref snap, -GridSize.x, -GridSize.z); // check AdjustPosition()
                else if (snap.magnitude < InnerScale / 2) // or the snapping make the point out of shape area from inside
                    AdjustPosition(ref snap, GridSize.x, GridSize.z); // check AdjustPosition()
            }
            // setup the last axis 'Y' 
            if (GridSize.y > 0) pos.y = Mathf.Round(pos.y / GridSize.y) * GridSize.y; // snap y axis point
            if (pos.y > Hight) pos.y -= GridSize.y; // check if the snapping make the point out of shape area
            return new Vector3(snap.x * Mathf.Sign(pos.x), pos.y, snap.z * Mathf.Sign(pos.z)); // reset the sign and return snapped point
        }

        // this Adjust method make sure snapped point not exeeded the area of the shape
        void AdjustPosition(ref Vector3 tmp, float xAdjust, float zAdjust)
        {
            bool selection = Random.value < 0.5f; // random selection
            if (selection) tmp.x += xAdjust; else tmp.z += zAdjust; // select a random axis to reverse it's snap

            // if the above snap still not bring the point inside shape area
            if ((SpawnShape == SpawnShapeType.Circle && (tmp.magnitude > OuterScale / 2 || tmp.magnitude < InnerScale / 2)) ||
                (SpawnShape == SpawnShapeType.Square && tmp.x < InnerScale / 2 && tmp.z < InnerScale / 2))
            {
                // fixing one axis is enough in 99.99% of cases(0.01 id on square area)
                // fixing both in one step not giving accurate results so try fixing one axis...
                if (selection)
                {
                    tmp.x -= xAdjust; // return the previus snap adjust
                    tmp.z += zAdjust; // snap adjust the other axis
                }
                else
                {
                    tmp.z -= zAdjust; // return the previus snap adjust
                    tmp.x += xAdjust; // snap adjust the other axis
                }
            }
            //... if it still not fixed... // 
            if (SpawnShape == SpawnShapeType.Square) // some times in square shape both axis snapped outside so check both axis again
            {
                if (tmp.x > OuterScale / 2) tmp.x -= GridSize.x;
                if (tmp.z > OuterScale / 2) tmp.z -= GridSize.z;
            }
        }
        #endregion

        #region Public Methods
        public void EnableState(bool enable) // main behavior enable and disable controller
        {
            if (enable == Enable) return; // break if it was already enabled
            Enable = enable;
            if (enable) // On Enable... do
            {
                StartCoroutine(MainSpawnCycle());
                StartCoroutine(AccelerationControl());
            }
            else // On Disable... do
            {
                StopAllCoroutines();
            }
        }
        //_________________________________________________
        // Create a custom cycle next to the main spawn system (even if the spawn system is disabled)
        public void CreateCustomCycle(int Amount, float DelayBetweenEachSpawn) // if DelayBetweenEachSpawn == 0 this cycle type will be single swarm
        {
            StartCoroutine(CustomSpawnCycle(Amount, DelayBetweenEachSpawn));
        }
        //_________________________________________________
        public Vector3 GeneratePoint() // generate a spawn point and return it
        {
            return PositionGenerator(); // note that generated point not added to spawn data store lists
        }
        //_________________________________________________
        public Vector3 GenerateEulerAngle(Vector3 position) // generate a spawn rotation in Euler angle and return it
        {
            return RotationGenerator(position); // note that generated rotation not added to spawn data store lists
        }
        //_________________________________________________
        public Quaternion GenerateQuaternion(Vector3 position) // generate a spawn rotation in Quaternion and return it
        {
            return Quaternion.Euler(RotationGenerator(position)); // note that generated rotation not added to spawn data store lists
        }
        //_________________________________________________
        public List<GameObject> GetAllActiveOjectsInScene() // return a list of all spawned active objects in the scene
        {
            ActiveObjectsInScene.RemoveAll(item => item == null); // Clean the list from all null and destryed objects 
            return ActiveObjectsInScene; // and return it
        }
        //_________________________________________________
        public List<GameObject> GetCurrentSpawnedObjects() // return a list of all spawned active objects from the current Cycle in the scene
        {
            CurrentSpawnObjects.RemoveAll(item => item == null);
            return CurrentSpawnObjects;
        }
        //_________________________________________________
        public GameObject GetLastSpawnedObject() // return the last spawned object from the system
        {
            if (ActiveObjectsInScene[^1] == null) Debug.LogWarning("Last Spawned object is destroyed, return the one before it");
            ActiveObjectsInScene.RemoveAll(item => item == null); // Clean the list from all null and destryed objects 
            if (ActiveObjectsInScene.Count == 0) Debug.LogWarning("The spawnd objects list is empty, all spawend objects is destroyed");
            return ActiveObjectsInScene[^1]; // if the list is empty warining message will pop and error after it
        }
        //_________________________________________________
        public List<Vector3> GetCurrentSpawnedPoints() // return a list of all positions generated from the current Cycle
        {
            return CurrentSpawnPoints;
        }
        //_________________________________________________
        public List<Vector3> GetCurrentSpawnAngles() // return a list of all rotations in euler generated from the current Cycle
        {
            return CurrentSpawnAngles;
        }
        //_________________________________________________
        #endregion

        #region Debug Setup

        [BigHeader("Debug Settings")] [Space]
        public bool EnableDebug = true;
        public bool HideEdges;
        [Min(12)] public int CircleSegments = 24;
        [Min(0)] public int SquareInnerSegmentsX4 = 2;
        [Min(.01f)] public float xSnapPointScale = .5f;
        public Color EdgeColor = Color.black;
        public Color PointColor = Color.black;

        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Hight = Mathf.Max(Hight, 0);
            Vector3 center = transform.position;
            if (!EnableDebug) return;
            switch (SpawnShape)
            {
                case SpawnShapeType.Circle:
                    if (!HideEdges) DrawCircleEdges(center, InnerScale / 2, OuterScale / 2, Hight, CircleSegments, EdgeColor);
                    if (EnableSnap)
                        DrawSnapPointsInCircle(center, InnerScale / 2, OuterScale / 2, Hight, GridSize, xSnapPointScale, PointColor);
                    else
                        DrawShadedAreaBetweenCircles(center, InnerScale / 2, OuterScale / 2, Hight, CircleSegments, PointColor);
                    break;
                case SpawnShapeType.Square:
                    if (!HideEdges) DrawSquareEdges(center, InnerScale, OuterScale, Hight, EdgeColor);
                    if (EnableSnap)
                        DrawSnapPointsInSquare(center, InnerScale, OuterScale, Hight, GridSize, xSnapPointScale, PointColor);
                    else
                        DrawShadedAreaBetweenSquares(center, InnerScale, OuterScale, Hight, SquareInnerSegmentsX4, PointColor);
                    break;
            }
        }

        void DrawCircleEdges(Vector3 center, float innerRadius, float outerRadius, float hight, int segments, Color color)
        {
            float angleStep = 360.0f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 innerPoint1 = center + new Vector3(Mathf.Cos(angle) * innerRadius, 0, Mathf.Sin(angle) * innerRadius);
                Vector3 innerPoint2 = center + new Vector3(Mathf.Cos(nextAngle) * innerRadius, 0, Mathf.Sin(nextAngle) * innerRadius);
                Vector3 outerPoint1 = center + new Vector3(Mathf.Cos(angle) * outerRadius, 0, Mathf.Sin(angle) * outerRadius);
                Vector3 outerPoint2 = center + new Vector3(Mathf.Cos(nextAngle) * outerRadius, 0, Mathf.Sin(nextAngle) * outerRadius);

                Debug.DrawLine(innerPoint1, innerPoint2, color);
                Debug.DrawLine(outerPoint1, outerPoint2, color);
                Debug.DrawLine(innerPoint1 + Vector3.up * hight, innerPoint2 + Vector3.up * hight, color);
                Debug.DrawLine(outerPoint1 + Vector3.up * hight, outerPoint2 + Vector3.up * hight, color);

                Debug.DrawLine(innerPoint1, innerPoint1 + Vector3.up * hight, color);
                Debug.DrawLine(innerPoint2, innerPoint2 + Vector3.up * hight, color);
                Debug.DrawLine(outerPoint1, outerPoint1 + Vector3.up * hight, color);
                Debug.DrawLine(outerPoint2, outerPoint2 + Vector3.up * hight, color);
            }
        }

        void DrawShadedAreaBetweenCircles(Vector3 center, float innerRadius, float outerRadius, float hight, int segments, Color color)
        {
            float angleStep = 360.0f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 innerPoint1 = center + new Vector3(Mathf.Cos(angle) * innerRadius, 0, Mathf.Sin(angle) * innerRadius);
                Vector3 innerPoint2 = center + new Vector3(Mathf.Cos(nextAngle) * innerRadius, 0, Mathf.Sin(nextAngle) * innerRadius);
                Vector3 outerPoint1 = center + new Vector3(Mathf.Cos(angle) * outerRadius, 0, Mathf.Sin(angle) * outerRadius);
                Vector3 outerPoint2 = center + new Vector3(Mathf.Cos(nextAngle) * outerRadius, 0, Mathf.Sin(nextAngle) * outerRadius);

                Debug.DrawLine(innerPoint1, outerPoint1, color);
                Debug.DrawLine(innerPoint2, outerPoint2, color);
                Debug.DrawLine(innerPoint1 + Vector3.up * hight, outerPoint1 + Vector3.up * hight, color);
                Debug.DrawLine(innerPoint2 + Vector3.up * hight, outerPoint2 + Vector3.up * hight, color);
            }
        }

        void DrawShadedAreaBetweenSquares(Vector3 center, float innerSize, float outerSize, float hight, int segments, Color color)
        {
            if (SquareInnerSegmentsX4 <= 0) return;
            Vector3[] innerSquare = new Vector3[4];
            Vector3[] outerSquare = new Vector3[4];

            innerSquare[0] = center + new Vector3(-innerSize / 2, 0, -innerSize / 2);
            innerSquare[1] = center + new Vector3(innerSize / 2, 0, -innerSize / 2);
            innerSquare[2] = center + new Vector3(innerSize / 2, 0, innerSize / 2);
            innerSquare[3] = center + new Vector3(-innerSize / 2, 0, innerSize / 2);

            outerSquare[0] = center + new Vector3(-outerSize / 2, 0, -outerSize / 2);
            outerSquare[1] = center + new Vector3(outerSize / 2, 0, -outerSize / 2);
            outerSquare[2] = center + new Vector3(outerSize / 2, 0, outerSize / 2);
            outerSquare[3] = center + new Vector3(-outerSize / 2, 0, outerSize / 2);

            for (int i = 0; i < 4; i++)
            {
                if (segments > 0) Debug.DrawLine(innerSquare[i], outerSquare[i], color);
                if (hight > 0) Debug.DrawLine(innerSquare[i] + Vector3.up * hight, outerSquare[i] + Vector3.up * hight, color);
            }

            for (int i = 1; i < segments; i++)
            {
                float t = (float)i / segments;
                for (int j = 0; j < 4; j++)
                {
                    Vector3 innerPoint = Vector3.Lerp(innerSquare[j], innerSquare[(j + 1) % 4], t);
                    Vector3 outerPoint = Vector3.Lerp(outerSquare[j], outerSquare[(j + 1) % 4], t);
                    Debug.DrawLine(innerPoint, outerPoint, color);
                    Debug.DrawLine(innerPoint + Vector3.up * hight, outerPoint + Vector3.up * hight, color);
                }
            }
        }

        void DrawSquareEdges(Vector3 center, float innerSize, float outerSize, float hight, Color color)
        {
            Vector3[] innerSquare = new Vector3[4];
            Vector3[] outerSquare = new Vector3[4];

            innerSquare[0] = center + new Vector3(-innerSize / 2, 0, -innerSize / 2);
            innerSquare[1] = center + new Vector3(innerSize / 2, 0, -innerSize / 2);
            innerSquare[2] = center + new Vector3(innerSize / 2, 0, innerSize / 2);
            innerSquare[3] = center + new Vector3(-innerSize / 2, 0, innerSize / 2);

            outerSquare[0] = center + new Vector3(-outerSize / 2, 0, -outerSize / 2);
            outerSquare[1] = center + new Vector3(outerSize / 2, 0, -outerSize / 2);
            outerSquare[2] = center + new Vector3(outerSize / 2, 0, outerSize / 2);
            outerSquare[3] = center + new Vector3(-outerSize / 2, 0, outerSize / 2);

            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(innerSquare[i], innerSquare[(i + 1) % 4], color);
                Debug.DrawLine(outerSquare[i], outerSquare[(i + 1) % 4], color);
                Debug.DrawLine(innerSquare[i] + Vector3.up * hight, innerSquare[(i + 1) % 4] + Vector3.up * hight, color);
                Debug.DrawLine(outerSquare[i] + Vector3.up * hight, outerSquare[(i + 1) % 4] + Vector3.up * hight, color);

                Debug.DrawLine(innerSquare[i], innerSquare[i] + Vector3.up * hight, color);
                Debug.DrawLine(innerSquare[(i + 1) % 4], innerSquare[(i + 1) % 4] + Vector3.up * hight, color);
                Debug.DrawLine(outerSquare[i], outerSquare[i] + Vector3.up * hight, color);
                Debug.DrawLine(outerSquare[(i + 1) % 4], outerSquare[(i + 1) % 4] + Vector3.up * hight, color);
            }
        }

        void DrawSnapPointsInCircle(Vector3 center, float innerRadius, float outerRadius, float hight, Vector3 gridSize, float xPointScale, Color color)
        {
            for (float x = -outerRadius; x <= outerRadius; x += gridSize.x)
            {
                for (float z = -outerRadius; z <= outerRadius; z += gridSize.z)
                {
                    Vector3 point = new(Mathf.Round((x + .01f) / gridSize.x) * gridSize.x, 0, Mathf.Round((z + .01f) / gridSize.z) * gridSize.z);
                    if (point.magnitude >= innerRadius && point.magnitude <= outerRadius)
                    {
                        Debug.DrawLine(center + point + new Vector3(-xPointScale / 2, 0, -xPointScale / 2), center + point + new Vector3(xPointScale / 2, 0, xPointScale / 2), color);
                        Debug.DrawLine(center + point + new Vector3(-xPointScale / 2, 0, xPointScale / 2), center + point + new Vector3(xPointScale / 2, 0, -xPointScale / 2), color);
                        if (gridSize.y == 0)
                        {
                            Debug.DrawLine(center + point, center + point + Vector3.up * hight, color);
                            Debug.DrawLine(center + point + new Vector3(-xPointScale / 2, hight, -xPointScale / 2), center + point + new Vector3(xPointScale / 2, hight, xPointScale / 2), color);
                            Debug.DrawLine(center + point + new Vector3(-xPointScale / 2, hight, xPointScale / 2), center + point + new Vector3(xPointScale / 2, hight, -xPointScale / 2), color);
                        }
                        else for (float i = gridSize.y; i < hight; i += gridSize.y)
                            {
                                Debug.DrawLine(center + point + new Vector3(-xPointScale / 2, i, -xPointScale / 2), center + point + new Vector3(xPointScale / 2, i, xPointScale / 2), color);
                                Debug.DrawLine(center + point + new Vector3(-xPointScale / 2, i, xPointScale / 2), center + point + new Vector3(xPointScale / 2, i, -xPointScale / 2), color);
                            }
                    }
                }
            }
        }

        void DrawSnapPointsInSquare(Vector3 center, float innerSize, float outerSize, float hight, Vector3 gridSize, float xPointScale, Color color)
        {
            for (float x = -outerSize / 2; x <= outerSize / 2; x += gridSize.x)
            {
                for (float z = -outerSize / 2; z <= outerSize / 2; z += gridSize.z)
                {
                    Vector3 point = center + new Vector3(Mathf.Round((x + .01f) / gridSize.x) * gridSize.x, 0, Mathf.Round((z + .01f) / gridSize.z) * gridSize.z);
                    if (Mathf.Abs(point.x - center.x) <= outerSize / 2 && Mathf.Abs(point.z - center.z) <= outerSize / 2 &&
                        (Mathf.Abs(point.x - center.x) >= innerSize / 2 || Mathf.Abs(point.z - center.z) >= innerSize / 2))
                    {
                        Debug.DrawLine(point + new Vector3(-xPointScale / 2, 0, -xPointScale / 2), point + new Vector3(xPointScale / 2, 0, xPointScale / 2), color);
                        Debug.DrawLine(point + new Vector3(-xPointScale / 2, 0, xPointScale / 2), point + new Vector3(xPointScale / 2, 0, -xPointScale / 2), color);
                        if (gridSize.y == 0)
                        {
                            Debug.DrawLine(point, point + Vector3.up * hight, color);
                            Debug.DrawLine(point + new Vector3(-xPointScale / 2, hight, -xPointScale / 2), point + new Vector3(xPointScale / 2, hight, xPointScale / 2), color);
                            Debug.DrawLine(point + new Vector3(-xPointScale / 2, hight, xPointScale / 2), point + new Vector3(xPointScale / 2, hight, -xPointScale / 2), color);
                        }
                        else for (float i = gridSize.y; i < hight; i += gridSize.y)
                            {
                                Debug.DrawLine(point + new Vector3(-xPointScale / 2, i, -xPointScale / 2), point + new Vector3(xPointScale / 2, i, xPointScale / 2), color);
                                Debug.DrawLine(point + new Vector3(-xPointScale / 2, i, xPointScale / 2), point + new Vector3(xPointScale / 2, i, -xPointScale / 2), color);
                            }
                    }
                }
            }
        }
        #endif
        #endregion
    }
}