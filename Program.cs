using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HedgeLib.Sets;
using System.Threading;
using System.Linq;
using HedgeLib;
using System.Xml.Linq;
using System.Numerics;
namespace GensToForces
{
    public class Program
    {
        public class GensParam : SetObjectParam
        {
            public string Name;
        }

        public List<GensParam> Params = new List<GensParam>();
        public static void Main(string[] args)
        {
            var GensObjToGismo = new List<string>();

            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-us");
            if (args.Length < 1 || !File.Exists(args[0]))
            {
                Program.ShowHelp();
                return;
            }
            string text = args[0];
            string extension = Path.GetExtension(text);
            if (extension == null || extension.ToLower() != ".xml")
            {
                Program.ShowHelp();
                return;
            }
            Console.WriteLine("Loading Generations Templates...");
            if (!Directory.Exists(Path.Combine("Templates\\Generations")))
            {
                var oldCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NO TEMPLATES WERE FOUND FOR GENERATIONS, ABORTING!!");
                Console.ForegroundColor = oldCol;
                Console.ReadKey();
                return;
            }

            var templates = SetObjectType.LoadObjectTemplates("Templates", "Generations");
            if (templates.Count == 0)
            {
                var oldCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NO TEMPLATES WERE FOUND FOR GENERATIONS, ABORTING!!");
                Console.ForegroundColor = oldCol;
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Loading Generations SetData...");
            GensSetData gensSetData = new GensSetData();
            ForcesSetData forcesSetData = new ForcesSetData();
            using (var fileStream = File.OpenRead(text))
            {
                Load(gensSetData, fileStream, templates);
            }
            Console.WriteLine("Creating Objects...");

            foreach (SetObject setObject_Gens in gensSetData.Objects)
            {
                //SuperSonic16's Entries start here - Fixed by SWS90 to reflect final Forces templates, and to more accurately convert SET data.
                SetObject setObject_Forces = new SetObject();
                if (setObject_Gens.ObjectType == "Ring")
                {
                    setObject_Forces.ObjectType = "ObjRing";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 8;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ResetTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Type 0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(1, typeof(byte)))); //RotateType 1

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "DashRing")
                {
                    setObject_Forces.ObjectType = "ObjDashRing";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;

                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 120;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float KeepVelocityDistance = float.Parse(GetParamByName("KeepVelocityDistance", setObject_Gens.Parameters).Data + "") / 10f;
                    float Speed = float.Parse(GetParamByName("FirstSpeed", setObject_Gens.Parameters).Data + "") * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 0u)); // RingType - Dash Hoop
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), KeepVelocityDistance));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsVisible
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsPositionConstant
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsVelocityConstant
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // DoesCauseSpin
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "RainbowRing")
                {
                    setObject_Forces.ObjectType = "ObjDashRing";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;

                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 120;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float KeepVelocityDistance = float.Parse(GetParamByName("KeepVelocityDistance", setObject_Gens.Parameters).Data + "") / 10f;
                    float Speed = float.Parse(GetParamByName("FirstSpeed", setObject_Gens.Parameters).Data + "") * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 1u)); // RingType - Rainbow Rings
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), KeepVelocityDistance));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsVisible
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsPositionConstant
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsVelocityConstant
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // DoesCauseSpin
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };
                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "SuperRing")
                {
                    setObject_Forces.ObjectType = "ObjSuperRing";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 2;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Type
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(1, typeof(byte)))); //RotateType

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "DashPanel")
                {
                    setObject_Forces.ObjectType = "ObjDashPanel";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float Speed = float.Parse(GetParamByName("Speed", setObject_Gens.Parameters).Data + "") * 10f;
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsVisible
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsSideView
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsDirectionPath
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsForceLanding
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "GrindDashPanel")
                {
                    setObject_Forces.ObjectType = "ObjGrindBooster";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float Speed = float.Parse(GetParamByName("Speed", setObject_Gens.Parameters).Data + "") * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsVisible
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsReversed
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "UpReel")
                {
                    setObject_Forces.ObjectType = "ObjUpReel";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 120;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float Length = float.Parse(GetParamByName("Length", setObject_Gens.Parameters).Data + "");
                    float MaxSpeed = float.Parse(GetParamByName("UpSpeedMax", setObject_Gens.Parameters).Data + "") * 10f;
                    float Speed = float.Parse(GetParamByName("ImpulseVelocity", setObject_Gens.Parameters).Data + "") * 10f;
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Length));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), MaxSpeed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsOneTimeUp
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsEventDriven
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "RedMedal")
                {
                    setObject_Forces.ObjectType = "ObjRedRing";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 8;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    int RedRingID = int.Parse(GetParamByName("MedalID", setObject_Gens.Parameters).Data + "");

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(int), RedRingID));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Type
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(1, typeof(byte)))); //RotateType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // SeparateTranslucent

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "FallDeadCollision")
                {
                    setObject_Forces.ObjectType = "ObjFallDeadTrigger";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 32;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float Height = float.Parse(GetParamByName("Collision_Height", setObject_Gens.Parameters).Data + "") * 10f;
                    float Width = float.Parse(GetParamByName("Collision_Width", setObject_Gens.Parameters).Data + "") * 10f;
                    var Size = new Vector3(Width, Height, 1);

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), Size));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Trigger - Range
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), -1f)); //Distance

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "PointMarker")
                {
                    setObject_Forces.ObjectType = "ObjPointMarker";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 12;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float Height = float.Parse(GetParamByName("Height", setObject_Gens.Parameters).Data + "") * 10f;
                    float Width = float.Parse(GetParamByName("Width", setObject_Gens.Parameters, new SetObjectParam(typeof(float), 5f)).Data + "") * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Width));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Height));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Type: 0 = 3D

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "GoalRing")
                {
                    setObject_Forces.ObjectType = "ObjGoalTrigger";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    float Width = float.Parse(GetParamByName("Radius", setObject_Gens.Parameters).Data + "") * 10f;
                    float Height = float.Parse(GetParamByName("Radius", setObject_Gens.Parameters).Data + "") * 10f;
                    float Depth = float.Parse(GetParamByName("Radius", setObject_Gens.Parameters).Data + "") * 10f;
                    var PlateOffsetPos = new Vector3(0, 0, 0);
                    var PlateOffsetRot = new Vector3(0, 0, 0);

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Shape:1 - sphere
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //BasePoint:0 - BASE_CENTER
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //CollisionFilter:0 - Default
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Width));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Height));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Depth));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 3f)); //GoalTime 
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //PlayerAction
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 0u)); // PathUID
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //PlateModelType:0 - Plate Model #1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //PlateActionType:0 - Idle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //PlateSpeedType:0 - Use PlateMoveSpeed Value
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //PlateAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), PlateOffsetPos));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), PlateOffsetRot));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 0u)); // PlatePathUID
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 10f)); //PlateMoveSpeed
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 1000f)); //PlateMoveDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 1f)); //PlateScale
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 10u)); // TextureResolutionScale

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "JumpBoard")
                {
                    setObject_Forces.ObjectType = "ObjJumpBoard";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 120;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float ImpulseSpeedOnNormal = float.Parse(GetParamByName("ImpulseSpeedOnNormal", setObject_Gens.Parameters).Data + "") * 10f;
                    float ImpulseSpeedOnBoost = float.Parse(GetParamByName("ImpulseSpeedOnBoost", setObject_Gens.Parameters).Data + "") * 10f;
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float AngleType = float.Parse(GetParamByName("AngleType", setObject_Gens.Parameters).Data + "");
                    uint Size = 0;

                    if (AngleType == 0f) Size = 0;
                    if (AngleType == 1f) Size = 0;
                    if (AngleType == 2f) Size = 1;
                    if (AngleType == 3f) Size = 2;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ImpulseSpeedOnNormal));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ImpulseSpeedOnBoost));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 2.5f)); //MotionTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //DrawType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), (byte)Size));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            if (AngleType == 0f)
                            {
                                float Yaw_trans2_JumpBoard = (float)(180 / 180.0 * Math.PI);
                                float Pitch_trans2_JumpBoard = (float)(15 / 180.0 * Math.PI);
                                trans2.Rotation = Quaternion.Multiply(trans2.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans2_JumpBoard, Pitch_trans2_JumpBoard, 0)); //Y,X,Z
                            }
                            else
                            {
                                float Yaw_trans2_JumpBoard = (float)(180 / 180.0 * Math.PI);
                                trans2.Rotation = Quaternion.Multiply(trans2.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans2_JumpBoard, 0, 0)); //Y,X,Z
                            }
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    if (AngleType == 0f)
                    {
                        float Yaw_trans_JumpBoard = (float)(180 / 180.0 * Math.PI);
                        float Pitch_trans_JumpBoard = (float)(15 / 180.0 * Math.PI);
                        trans.Rotation = Quaternion.Multiply(trans.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans_JumpBoard, Pitch_trans_JumpBoard, 0)); //Y,X,Z
                    }
                    else
                    {
                        float Yaw_trans_JumpBoard = (float)(180 / 180.0 * Math.PI);
                        trans.Rotation = Quaternion.Multiply(trans.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans_JumpBoard, 0, 0)); //Y,X,Z
                    }
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "JumpBoard3D")
                {
                    setObject_Forces.ObjectType = "ObjJumpBoard";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 120;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    float ImpulseSpeedOnNormal = float.Parse(GetParamByName("ImpulseSpeedOnNormal", setObject_Gens.Parameters).Data + "") * 10f;
                    float ImpulseSpeedOnBoost = float.Parse(GetParamByName("ImpulseSpeedOnBoost", setObject_Gens.Parameters).Data + "") * 10f;
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float SizeType = float.Parse(GetParamByName("SizeType", setObject_Gens.Parameters).Data + "");

                    uint Size = 1;
                    if (SizeType == 0f) Size = 1;
                    if (SizeType == 1f) Size = 2;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ImpulseSpeedOnNormal));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ImpulseSpeedOnBoost));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 2.5f)); //MotionTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //DrawType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), (byte)Size));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            float Yaw_trans2 = (float)(180 / 180.0 * Math.PI);
                            trans2.Rotation = Quaternion.Multiply(trans2.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans2, 0, 0)); //Y,X,Z
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    float Yaw_trans = (float)(180 / 180.0 * Math.PI);
                    trans.Rotation = Quaternion.Multiply(trans.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans, 0, 0)); //Y,X,Z
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "AdlibTrickJump")
                {
                    setObject_Forces.ObjectType = "ObjJumpBoard";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 120;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    float ImpulseSpeedOnNormal = float.Parse(GetParamByName("ImpulseSpeedOnNormal", setObject_Gens.Parameters).Data + "") * 10f;
                    float ImpulseSpeedOnBoost = float.Parse(GetParamByName("ImpulseSpeedOnBoost", setObject_Gens.Parameters).Data + "") * 10f;
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float SizeType = float.Parse(GetParamByName("SizeType", setObject_Gens.Parameters).Data + "");

                    uint Size = 2;
                    if (SizeType == 0f) Size = 1;
                    if (SizeType == 1f) Size = 2;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ImpulseSpeedOnNormal));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ImpulseSpeedOnBoost));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 2.5f)); //MotionTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //DrawType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), (byte)Size));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            float Yaw_trans2 = (float)(180 / 180.0 * Math.PI);
                            trans2.Rotation = Quaternion.Multiply(trans2.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans2, 0, 0)); //Y,X,Z
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    float Yaw_trans = (float)(180 / 180.0 * Math.PI);
                    trans.Rotation = Quaternion.Multiply(trans.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans, 0, 0)); //Y,X,Z
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemySpinner")
                {
                    setObject_Forces.ObjectType = "EnmPotos";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 56;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float MoveSpeed = float.Parse(GetParamByName("MoveVel", setObject_Gens.Parameters).Data + "") * 10f;
                    float RespawnTime = float.Parse(GetParamByName("RebirthTime", setObject_Gens.Parameters).Data + "");

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //StartType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), MoveSpeed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // LocateList

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackInterval
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchBoxWidth
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchBoxHeight
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchBoxDepth
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), RespawnTime));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // In3D

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemySpinner2D")
                {
                    setObject_Forces.ObjectType = "EnmPotos";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 56;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float MoveSpeed = float.Parse(GetParamByName("MoveVel", setObject_Gens.Parameters).Data + "") * 10f;
                    float RespawnTime = float.Parse(GetParamByName("RebirthTime", setObject_Gens.Parameters).Data + "");

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //StartType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), MoveSpeed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // LocateList

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackInterval
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchBoxWidth
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchBoxHeight
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchBoxDepth
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), RespawnTime));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // In3D

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemyPawn2D" || setObject_Gens.ObjectType == "EnemyPawnGun2D" || setObject_Gens.ObjectType == "EnemyPawnLance2D" || setObject_Gens.ObjectType == "EnemyPawnPla2D" || setObject_Gens.ObjectType == "EnemyEFighter2D" || setObject_Gens.ObjectType == "EnemyEFighter2D" || setObject_Gens.ObjectType == "EnemyEFighterSword2D")
                {
                    setObject_Forces.ObjectType = "EnemyEggPawn";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 40;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //WeaponType - Laser Machine Gun
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //ApproachLimit
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 400f)); //SearchDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //MoveDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // ShotCoolDown
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // AttackConst
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackConstAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // In3D
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsEventDriven
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // HasGravity
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsFallStart
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsTreadGrass
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // UpdateMaterial
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsStatic
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //BehaviorType - Normal
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ToSVPathDistance

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemyPawn3D" || setObject_Gens.ObjectType == "EnemyPawnGun3D" || setObject_Gens.ObjectType == "EnemyPawnLance3D" || setObject_Gens.ObjectType == "EnemyPawnPla3D" || setObject_Gens.ObjectType == "EnemyEFighter3D" || setObject_Gens.ObjectType == "EnemyEFighter3D" || setObject_Gens.ObjectType == "EnemyEFighter3D" || setObject_Gens.ObjectType == "EnemyEFighterSword3D")
                {
                    setObject_Forces.ObjectType = "EnemyEggPawn";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 40;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //WeaponType - Laser Machine Gun
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //ApproachLimit
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 400f)); //SearchDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //MoveDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // ShotCoolDown
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // AttackConst
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackConstAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // In3D
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsEventDriven
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // HasGravity
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsFallStart
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsTreadGrass
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // UpdateMaterial
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsStatic
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //BehaviorType - Normal
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ToSVPathDistance

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemyEFighter3DGun")
                {
                    setObject_Forces.ObjectType = "EnemyEggPawn";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = 1000f;
                    uint RBL = 40;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //WeaponType - Laser Machine Gun
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //ApproachLimit
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 400f)); //SearchDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //MoveDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // ShotCoolDown
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // AttackConst
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackConstAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // In3D
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsEventDriven
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // HasGravity
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsFallStart
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsTreadGrass
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // UpdateMaterial
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // IsStatic
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //BehaviorType - Normal
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ToSVPathDistance

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemyAeroCannon")
                {
                    setObject_Forces.ObjectType = "EnemyBeeton";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 400f)); //SearchDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //SearchMoveSpeed
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ViewBoxHalfLengthX
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ViewBoxHalfLengthY
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ViewBoxHalfLengthZ
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), new Vector3(0, 0, 0)));//ViewBoxOffset
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackRangeRatio
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackRangeHeightRatio
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackRangeOffset
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //DoesMove
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //CheckShielding
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //AttackConst
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AttackConstAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //StraightDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //RespawnTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), new Vector3(0, 0, 0)));//RespawnOffsetPos
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 0u));//RespawnPathUID
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); //In3D
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //IsEscape
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //IsEventDriven

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "EnemyELauncher2D" || setObject_Gens.ObjectType == "EnemyELauncher3D")
                {
                    setObject_Forces.ObjectType = "ObjResearchMissilePod";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 52;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    //
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //TimeType
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 3f)); //ShootingDelay
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //InitialDelay
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 300f)); //AttackDistance
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 90f)); //ShotAngleMin
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 90f)); //ShotAngleMax
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 240f)); //ShotSpeed
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0.6f)); //ShotTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 100f)); //MissileSpeed
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0.5f)); //MissileAccelTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 1f)); //MissileHomingWait
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(int), 5)); //MissileHomingRotation
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsEventDriven

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }

                else if (setObject_Gens.ObjectType == "WideSpring")
                {
                    setObject_Forces.ObjectType = "ObjWideSpring";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float Speed = float.Parse(GetParamByName("FirstSpeed", setObject_Gens.Parameters).Data + "") * 10f;
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float KeepVelocityDistance = float.Parse(GetParamByName("KeepVelocityDistance", setObject_Gens.Parameters).Data + "") * 10f;
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), KeepVelocityDistance));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "ObjectPhysics")
                {
                    string type = GetParamByName("Type", setObject_Gens.Parameters).Data + "";
                    if (type == "ThornCylinder2M")
                    {
                        setObject_Forces.ObjectType = "ObjThornCylinder";
                        setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                        float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                        uint RBL = 2;
                        setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                        setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                        setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Length
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false));//Rigidbody

                        List<SetObjectParam> parameters = setObject_Forces.Parameters;
                        string objectType = setObject_Forces.ObjectType;
                        if (setObject_Gens.Children != null)
                        {
                            foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                            {
                                var trans2 = GenTransform(setObjectTransform);
                                float Yaw_trans2 = (float)(-90 / 180.0 * Math.PI);
                                trans2.Rotation = Quaternion.Multiply(trans2.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans2, 0, 0)); //Y,X,Z
                                SetObject item = new SetObject
                                {
                                    ObjectType = objectType,
                                    ObjectID = setObject_Forces.ObjectID,
                                    Parameters = parameters,
                                    Transform = trans2,
                                    CustomData = setObject_Forces.CustomData

                                };

                                forcesSetData.Objects.Add(item);
                            }
                        }

                        var trans = GenTransform(setObject_Gens.Transform);
                        float Yaw_trans = (float)(-90 / 180.0 * Math.PI);
                        trans.Rotation = Quaternion.Multiply(trans.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans, 0, 0)); //Y,X,Z
                        SetObject item2 = new SetObject
                        {
                            ObjectType = objectType,
                            ObjectID = setObject_Forces.ObjectID,
                            Parameters = parameters,
                            Transform = trans,
                            CustomData = setObject_Forces.CustomData
                        };
                        forcesSetData.Objects.Add(item2);
                    }
                    else if (type == "ThornCylinder3M")
                    {
                        setObject_Forces.ObjectType = "ObjThornCylinder";
                        setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                        float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                        uint RBL = 2;
                        setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                        setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                        setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(1, typeof(byte)))); //Length
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false));//Rigidbody

                        List<SetObjectParam> parameters = setObject_Forces.Parameters;
                        string objectType = setObject_Forces.ObjectType;
                        if (setObject_Gens.Children != null)
                        {
                            foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                            {
                                var trans2 = GenTransform(setObjectTransform);
                                float Yaw_trans2 = (float)(-90 / 180.0 * Math.PI);
                                trans2.Rotation = Quaternion.Multiply(trans2.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans2, 0, 0)); //Y,X,Z
                                SetObject item = new SetObject
                                {
                                    ObjectType = objectType,
                                    ObjectID = setObject_Forces.ObjectID,
                                    Parameters = parameters,
                                    Transform = trans2,
                                    CustomData = setObject_Forces.CustomData

                                };

                                forcesSetData.Objects.Add(item);
                            }
                        }

                        var trans = GenTransform(setObject_Gens.Transform);
                        float Yaw_trans = (float)(-90 / 180.0 * Math.PI);
                        trans.Rotation = Quaternion.Multiply(trans.Rotation, Quaternion.CreateFromYawPitchRoll(Yaw_trans, 0, 0)); //Y,X,Z
                        SetObject item2 = new SetObject
                        {
                            ObjectType = objectType,
                            ObjectID = setObject_Forces.ObjectID,
                            Parameters = parameters,
                            Transform = trans,
                            CustomData = setObject_Forces.CustomData
                        };
                        forcesSetData.Objects.Add(item2);
                    }
                    else if (type == "IronBox2" || type == "IronBox")
                    {
                        setObject_Forces.ObjectType = "ObjIronBox";
                        setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                        float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                        uint RBL = 16;
                        setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                        setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                        setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(int), 1)); // BoxNumX
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(int), 1)); // BoxNumY
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(int), 1)); // BoxNumZ
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); // HasShadow

                        List<SetObjectParam> parameters = setObject_Forces.Parameters;
                        string objectType = setObject_Forces.ObjectType;
                        if (setObject_Gens.Children != null)
                        {
                            foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                            {
                                var trans2 = GenTransform(setObjectTransform);
                                SetObject item = new SetObject
                                {
                                    ObjectType = objectType,
                                    ObjectID = setObject_Forces.ObjectID,
                                    Parameters = parameters,
                                    Transform = trans2,
                                    CustomData = setObject_Forces.CustomData

                                };

                                forcesSetData.Objects.Add(item);
                            }
                        }

                        var trans = GenTransform(setObject_Gens.Transform);
                        SetObject item2 = new SetObject
                        {
                            ObjectType = objectType,
                            ObjectID = setObject_Forces.ObjectID,
                            Parameters = parameters,
                            Transform = trans,
                            CustomData = setObject_Forces.CustomData
                        };
                        forcesSetData.Objects.Add(item2);
                    }
                    else
                    {
                        string type2 = GetParamByName("Type", setObject_Gens.Parameters).Data + "";
                        setObject_Forces.ObjectType = "ObjGismo";
                        setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                        uint RBL = 56;
                        setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), 0f));
                        setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), 0f));
                        setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), type2));
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 1f)); //Scale
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "")); //GITextureName
                        setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "")); //GIOcclusionTextureName

                        List<SetObjectParam> parameters = setObject_Forces.Parameters;
                        string objectType = setObject_Forces.ObjectType;
                        if (setObject_Gens.Children != null)
                        {
                            foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                            {
                                var trans2 = GenTransform(setObjectTransform);
                                SetObject item = new SetObject
                                {
                                    ObjectType = objectType,
                                    ObjectID = setObject_Forces.ObjectID,
                                    Parameters = parameters,
                                    Transform = trans2,
                                    CustomData = setObject_Forces.CustomData

                                };

                                forcesSetData.Objects.Add(item);
                            }
                        }
                        var trans = GenTransform(setObject_Gens.Transform);
                        SetObject item2 = new SetObject
                        {
                            ObjectType = objectType,
                            ObjectID = setObject_Forces.ObjectID,
                            Parameters = parameters,
                            Transform = trans,
                            CustomData = setObject_Forces.CustomData
                        };
                        forcesSetData.Objects.Add(item2);
                    }

                }
                else if (setObject_Gens.ObjectType == "SonicSpawn")
                {
                    bool Active = bool.Parse(setObject_Gens.Parameters[0].Data + "");
                    if (!Active) //If not active
                        continue;
                    setObject_Forces.ObjectType = "ObjStartPosition";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    uint RBL = 16;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), 1000f));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), 1000f));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //StartType

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Convert.ChangeType(0, typeof(float)))); //Speed
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Convert.ChangeType(0, typeof(float)))); //Time
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Convert.ChangeType(0, typeof(float)))); //OutOfControl

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "SetRigidBody")
                {
                    setObject_Forces.ObjectType = "ObjSetRigidBody";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 32;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float Width = float.Parse(GetParamByName("Width", setObject_Gens.Parameters).Data + "") * 10f;
                    float Height = float.Parse(GetParamByName("Height", setObject_Gens.Parameters).Data + "") * 10f;
                    float Length = float.Parse(GetParamByName("Length", setObject_Gens.Parameters).Data + "") * 10f;
                    var Size = new Vector3(Width, Height, Length);

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), Size));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); //IsEventDriven
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Action

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "Bomb")
                {
                    setObject_Forces.ObjectType = "ObjGismo";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 56;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "Bomb"));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 1f)); //Scale
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "")); //GITextureName
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "")); //GIOcclusionTextureName

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                //SWS90's New entries start here
                else if (setObject_Gens.ObjectType == "Item")
                {
                    setObject_Forces.ObjectType = "ObjWispCapsule";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(1, typeof(byte)))); // Type
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); // Reset Time
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //IsEventDriven
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Asteroid
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Lightning
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Cube
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Drill
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Burst
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Void
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //Hover
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event0
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event1
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "Spring" || setObject_Gens.ObjectType == "AirSpring" || setObject_Gens.ObjectType == "SpringFake")
                {
                    setObject_Forces.ObjectType = "ObjSpring";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 112;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float Speed = float.Parse(GetParamByName("FirstSpeed", setObject_Gens.Parameters).Data + "") * 10f;
                    float OutOfControl = float.Parse(GetParamByName("OutOfControl", setObject_Gens.Parameters).Data + "");
                    float KeepVelocityDistance = float.Parse(GetParamByName("KeepVelocityDistance", setObject_Gens.Parameters).Data + "") * 10;
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), OutOfControl));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), KeepVelocityDistance));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //IsEventOn
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //IsHorizon
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), true)); //IsVisible

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event0

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event1

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); //Event2

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "ChangeVolumeCamera")
                {
                    setObject_Forces.ObjectType = "ObjCameraVolume";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 40;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float EaseTimeEnter = float.Parse(GetParamByName("Ease_Time_Enter", setObject_Gens.Parameters).Data + "");
                    float EaseTimeLeave = float.Parse(GetParamByName("Ease_Time_Leave", setObject_Gens.Parameters).Data + "");
                    float Width = float.Parse(GetParamByName("Collision_Width", setObject_Gens.Parameters).Data + "") * 10f;
                    float Height = float.Parse(GetParamByName("Collision_Height", setObject_Gens.Parameters).Data + "") * 10f;
                    float Depth = float.Parse(GetParamByName("Collision_Length", setObject_Gens.Parameters).Data + "") * 10f;
                    uint Priority = uint.Parse(GetParamByName("Priority", setObject_Gens.Parameters).Data + "");
                    uint Camera = setObject_Gens.TargetID;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), Camera)); //Camera
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), Priority)); //Priority
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); //UseHighPriority
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), EaseTimeEnter));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), EaseTimeLeave));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //InterpolateTypeEnter
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //InterpolateTypeLeave
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //DefaultState
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Action
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //Shape
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //BasePoint

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Width));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Height));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Depth));

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "ObjCameraParallel")
                {
                    setObject_Forces.ObjectType = "ObjCameraFollow";

                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 96;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    bool IsCameraView = bool.Parse(GetParamByName("IsCameraView", setObject_Gens.Parameters).Data + "");

                    float FOV = float.Parse(GetParamByName("Fovy", setObject_Gens.Parameters).Data + "");
                    float ZRotation = float.Parse(GetParamByName("ZRot", setObject_Gens.Parameters).Data + ""); // * 10f;
                    float Distance = float.Parse(GetParamByName("Distance", setObject_Gens.Parameters).Data + "") * 10f;
                    float Yaw = float.Parse(GetParamByName("Yaw", setObject_Gens.Parameters).Data + "");
                    float Pitch = float.Parse(GetParamByName("Pitch", setObject_Gens.Parameters).Data + "") + 1.75f;

                    var TargetOffset = new Vector3(0, 0, 0);
                    var PlayerOffset = new Vector3(0, 14f, 0);

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), IsCameraView));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), FOV));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ZRotation));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Distance));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Yaw));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Pitch));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), TargetOffset));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //GravityOffset
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), PlayerOffset));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); //FollowType

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "ObjCameraPan")
                {
                    setObject_Forces.ObjectType = "ObjCameraPan";

                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 80;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    bool IsCameraView = bool.Parse(GetParamByName("IsCameraView", setObject_Gens.Parameters).Data + "");
                    float FOV = float.Parse(GetParamByName("Fovy", setObject_Gens.Parameters).Data + "");
                    float Distance = float.Parse(GetParamByName("Distance", setObject_Gens.Parameters).Data + "") * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), IsCameraView));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), FOV));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // EnableLimitAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //AzimuthLimitAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //ElevationLimitAngle
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); //GravityOffset
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), new Vector3(0, 0, 0)));//PlayerOffset
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), new Vector3(0, 0, 0)));//WorldOffset
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(sbyte), Convert.ChangeType(0, typeof(sbyte)))); //PositionMode 0 Fixed point
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), Distance));

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "ObjCameraFix")
                {
                    setObject_Forces.ObjectType = "ObjCameraFix";

                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 48;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));
                    //
                    bool IsCameraView = bool.Parse(GetParamByName("IsCameraView", setObject_Gens.Parameters).Data + "");

                    float FOV = float.Parse(GetParamByName("Fovy", setObject_Gens.Parameters).Data + "");
                    float ZRotation = float.Parse(GetParamByName("ZRot", setObject_Gens.Parameters).Data + "");
                    Vector3 TargetPosition = setObject_Gens.TargetPosition * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), IsCameraView));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), FOV));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), ZRotation));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(sbyte), Convert.ChangeType(0, typeof(sbyte)))); //TargetType - 0 TargetPosition
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(Vector3), TargetPosition));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(ForcesSetData.ObjectReference[]), new ForcesSetData.ObjectReference[0] { })); // TargetID
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // IsRotateUpDir

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "AutorunStartCollision" || setObject_Gens.ObjectType == "AutorunStartSimpleCollision")
                {
                    setObject_Forces.ObjectType = "AutorunTrigger";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 28;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float width = float.Parse(GetParamByName("Collision_Width", setObject_Gens.Parameters).Data + "") * 10f;
                    float height = float.Parse(GetParamByName("Collision_Height", setObject_Gens.Parameters).Data + "") * 10f;
                    float speed = float.Parse(GetParamByName("Speed", setObject_Gens.Parameters).Data + "") * 10f;


                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); // action - ACT_START
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); // move - MOVE_FREE
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), width));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), height));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 0u)); // pathID
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), speed));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); // outOfControlTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // forceFall

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else if (setObject_Gens.ObjectType == "AutorunFinishCollision" || setObject_Gens.ObjectType == "AutorunFinishSimpleCollision")
                {
                    setObject_Forces.ObjectType = "AutorunTrigger";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    float ForcesRangeInAndOut = float.Parse(GetParamByName("Range", setObject_Gens.Parameters).Data + "") * 10f;
                    uint RBL = 28;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), ForcesRangeInAndOut));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    float width = float.Parse(GetParamByName("Collision_Width", setObject_Gens.Parameters).Data + "") * 10f;
                    float height = float.Parse(GetParamByName("Collision_Height", setObject_Gens.Parameters).Data + "") * 10f;

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(1, typeof(byte)))); // action - ACT_FINISH
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(byte), Convert.ChangeType(0, typeof(byte)))); // move - MOVE_FREE
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), width));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), height));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(uint), 0u)); // pathID
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 0f)); // outOfControlTime
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(bool), false)); // forceFall

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
                else // Convert objects not yet in code to Forces ObjGismo
                {
                    string type2 = setObject_Gens.ObjectType;
                    setObject_Forces.ObjectType = "ObjGismo";
                    setObject_Forces.ObjectID = setObject_Gens.ObjectID;
                    GensObjToGismo.Add(setObject_Gens.ObjectType);
                    uint RBL = 56;
                    setObject_Forces.CustomData.Add("RangeIn", new SetObjectParam(typeof(float), 1f));
                    setObject_Forces.CustomData.Add("RangeOut", new SetObjectParam(typeof(float), 1f));
                    setObject_Forces.CustomData.Add("RawByteLength", new SetObjectParam(typeof(uint), RBL));

                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), type2));
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(float), 1f)); //Scale
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "")); //GITextureName
                    setObject_Forces.Parameters.Add(new SetObjectParam(typeof(string), "")); //GIOcclusionTextureName

                    List<SetObjectParam> parameters = setObject_Forces.Parameters;
                    string objectType = setObject_Forces.ObjectType;
                    if (setObject_Gens.Children != null)
                    {
                        foreach (SetObjectTransform setObjectTransform in setObject_Gens.Children)
                        {
                            var trans2 = GenTransform(setObjectTransform);
                            SetObject item = new SetObject
                            {
                                ObjectType = objectType,
                                ObjectID = setObject_Forces.ObjectID,
                                Parameters = parameters,
                                Transform = trans2,
                                CustomData = setObject_Forces.CustomData

                            };

                            forcesSetData.Objects.Add(item);
                        }
                    }

                    var trans = GenTransform(setObject_Gens.Transform);
                    SetObject item2 = new SetObject
                    {
                        ObjectType = objectType,
                        ObjectID = setObject_Forces.ObjectID,
                        Parameters = parameters,
                        Transform = trans,
                        CustomData = setObject_Forces.CustomData
                    };
                    forcesSetData.Objects.Add(item2);
                }
            }
            Console.WriteLine("Done Converting Objects!");
            string directoryName = Path.GetDirectoryName(text);
            string fileName = Path.GetFileName(text);
            string str = fileName.Substring(0, fileName.IndexOf('.'));
            string filePath = Path.Combine(directoryName, str + ".gedit");
            Console.WriteLine("Saving Forces Gedit...");
            forcesSetData.Save(filePath, true);
            Console.WriteLine("Forces Gedit Saved!");
            File.WriteAllLines("GensObjToGismo.txt", GensObjToGismo.Distinct().OrderBy(x => x));
            Console.WriteLine("Done!\nPress any key to close...");
            Console.ReadKey();
        }
        public static SetObjectParam GetParamByName(string name, List<SetObjectParam> objParams, SetObjectParam def = null)
        {
            foreach (var p in objParams)
            {
                if (p is GensParam gensParam)
                {
                    if (gensParam.Name == name)
                        return gensParam;
                }
            }
            if (name == "AttackRange")
                return GetParamByName("RadiusAttack", objParams);
            if (name == "RadiusAttack")
                return GetParamByName("RadiusAttackFar", objParams);
            return def;
        }

        // Methods
        public static void Load(GensSetData set, Stream fileStream,
            Dictionary<string, SetObjectType> objectTemplates)
        {
            var xml = XDocument.Load(fileStream);
            foreach (var element in xml.Root.Elements())
            {
                string elemName = element.Name.LocalName;
                if (elemName.ToLower() == "layerdefine")
                {
                    // TODO: Parse LayerDefine XML elements.
                }
                else
                {
                    // Read Parameters
                    var parameters = new List<SetObjectParam>();
                    var transform = new SetObjectTransform();
                    SetObjectTransform[] children = null;
                    uint? objID = null;
                    int tarID = 0;
                    Vector3 tarPos = new Vector3();

                    foreach (var paramElement in element.Elements())
                    {
                        // Load special parameters
                        string paramName = paramElement.Name.LocalName;
                        switch (paramName.ToLower())
                        {
                            case "position":
                                transform.Position = Helpers.XMLReadVector3(paramElement);
                                continue;

                            case "rotation":
                                transform.Rotation = Helpers.XMLReadQuat(paramElement);
                                continue;

                            case "setobjectid":
                                objID = Convert.ToUInt32(paramElement.Value);
                                continue;

                            case "multisetparam":
                                {
                                    var countElem = paramElement.Element("Count");
                                    if (countElem == null) continue;

                                    if (!int.TryParse(countElem.Value, out var childCount)) continue;
                                    var childObjs = new List<SetObjectTransform>();

                                    foreach (var specialElem in paramElement.Elements())
                                    {
                                        switch (specialElem.Name.LocalName.ToLower())
                                        {
                                            case "element":
                                                {
                                                    var indexElem = specialElem.Element("Index");
                                                    var posElem = specialElem.Element("Position");
                                                    var rotElem = specialElem.Element("Rotation");

                                                    if (indexElem == null || !int.TryParse(
                                                        indexElem.Value, out var index))
                                                        continue;

                                                    var pos = (posElem == null) ?
                                                        new Vector3() :
                                                        Helpers.XMLReadVector3(posElem);
                                                    var rot = (rotElem == null) ?
                                                        new Quaternion(0, 0, 0, 1) :
                                                        Helpers.XMLReadQuat(rotElem);

                                                    var childTransform = new SetObjectTransform()
                                                    {
                                                        Position = pos,
                                                        Rotation = rot
                                                    };
                                                    childObjs.Add(childTransform);
                                                    break;
                                                }

                                                // TODO: Parse other elements.
                                        }
                                    }

                                    children = childObjs.ToArray();
                                    continue;
                                }
                            case "target":
                                {
                                    var tarElem = paramElement.Element("SetObjectID");
                                    if (tarElem == null) continue;

                                    if (!int.TryParse(tarElem.Value, out tarID)) continue;
                                    continue;
                                }
                            case "targetposition":
                                {
                                    tarPos = paramElement.GetVector3();
                                    continue;
                                }
                        }

                        // Get the parameter's type.
                        SetObjectType templateType = null;
                        SetObjectTypeParam templateParam = null;
                        if (objectTemplates.ContainsKey(elemName))
                            templateType = objectTemplates[elemName];
                        if (templateType == null)
                            continue;
                        templateParam = templateType.GetParameter(paramName);
                        if (templateParam == null)
                            continue;
                        var paramType = templateParam.DataType;
                        if (paramType == null) continue;

                        // Read the parameter's data
                        object data =
                            (paramType == typeof(Vector3)) ? Helpers.XMLReadVector3(paramElement) :
                            (paramType == typeof(Quaternion)) ? Helpers.XMLReadQuat(paramElement) :
                            Helpers.ChangeType(paramElement.Value, paramType);

                        // Add the Parameter to the list
                        var param = new GensParam()
                        {
                            Data = data,
                            DataType = paramType,
                            Name = paramName
                        };
                        parameters.Add(param);
                    }

                    // Ensure Object has ID
                    if (!objID.HasValue)
                    {
                        Console.WriteLine("WARNING: {0} \"{1}\" {2}",
                            "Object of type", elemName,
                            "is missing it's object ID! Skipping this object...");
                        continue;
                    }

                    // Add Object to List
                    set.Objects.Add(new SetObject()
                    {
                        ObjectType = elemName,
                        Parameters = parameters,
                        Transform = transform,
                        Children = children ?? (new SetObjectTransform[0]),
                        ObjectID = objID.Value,
                        TargetID = (uint)tarID,
                        TargetPosition = tarPos
                    });
                }
            }
        }
        public static SetObjectTransform GenTransform(SetObjectTransform trans)
        {
            return new SetObjectTransform
            {
                Position = trans.Position * 10f,
                //Normalize Quaternions.
                Rotation = Quaternion.Normalize(trans.Rotation),
                Scale = trans.Scale
            };
        }
        public static void ShowHelp()
        {
            Console.WriteLine("GensToForcesSETConverter\n");
            Console.WriteLine("Originally made by Radfordhound to convert Rings.\nExtended by SuperSonic16, extended more and fixed by SWS90.");
            Console.WriteLine("Extra coding Help: Sajid and Skyth.");
            Console.WriteLine("Extra Help: Pixeljoch\n");
            Console.WriteLine("Usage:");
            Console.WriteLine("Drag and drop a Generations .set.xml onto GensToForcesSETConverter.exe, to get a .gedit file for use in Sonic Forces.");
            Console.WriteLine("Press any key to close...");
            Console.ReadKey();
        }
    }
}
