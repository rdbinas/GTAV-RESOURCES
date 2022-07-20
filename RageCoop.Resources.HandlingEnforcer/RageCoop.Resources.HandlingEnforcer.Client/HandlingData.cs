using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using GTA.Math;
using GTA;
using Newtonsoft.Json;

namespace RageCoop.Resources.HandlingEnforcer.Client
{
    internal class HandlingData
    {
        public HandlingData(XmlNode node)
        {
            foreach(XmlNode n in node.ChildNodes)
            {
                switch (n.Name)
                {
                    case "handlingName":
                        Name = n.InnerText;
                        break;
                    case "fMass":
                        Mass = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fPercentSubmerged":
                        PercentSubmerged = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fInitialDriveForce":
                        InitialDriveForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fDriveInertia":
                        DriveInertia = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fClutchChangeRateScaleUpShift":
                        ClutchChangeRateScaleUpShift = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fClutchChangeRateScaleDownShift":
                        ClutchChangeRateScaleDownShift = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fBrakeForce":
                        BrakeForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fHandBrakeForce":
                        HandBrakeForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSteeringLock":
                        SteeringLock = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionCurveMax":
                        TractionCurveMax = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionCurveMin":
                        TractionCurveMin = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionSpringDeltaMax":
                        TractionSpringDeltaMax = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fCamberStiffnesss":
                        CamberStiffness = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionBiasFront":
                        TractionBiasFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fTractionLossMult":
                        TractionLossMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionForce":
                        SuspensionForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionCompDamp":
                        SuspensionCompressionDamping = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionReboundDamp":
                        SuspensionReboundDamping = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionUpperLimit":
                        SuspensionUpperLimit = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionLowerLimit":
                        SuspensionLowerLimit = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionRaise":
                        SuspensionRaise = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSuspensionBiasFront":
                        SuspensionBiasFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fAntiRollBarForce":
                        AntiRollBarForce = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fAntiRollBarBiasFront":
                        AntiRollBarBiasFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fRollCentreHeightFront":
                        RollCenterHeightFront = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fRollCentreHeightRear":
                        RollCenterHeightRear = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fCollisionDamageMult":
                        CollisionDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fWeaponDamageMult":
                        WeaponDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fDeformationDamageMult":
                        DeformationDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fEngineDamageMult":
                        EngineDamageMultiplier = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fPetrolTankVolume":
                        PetrolTankVolume = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fOilVolume":
                        OilVolume = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSeatOffsetDistX":
                        SeatOffsetDistanceX = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSeatOffsetDistY":
                        SeatOffsetDistanceY = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "fSeatOffsetDistZ":
                        SeatOffsetDistanceZ = float.Parse(n.Attributes["value"].Value);
                        break;
                    case "nMonetaryValue":
                        MonetaryValue = int.Parse(n.Attributes["value"].Value);
                        break;
                    case "nInitialDriveGears":
                        InitialDriveGears = int.Parse(n.Attributes["value"].Value);
                        break;
                    case "vecCentreOfMassOffset":
                        CenterOfMassOffset = ToVec(n);
                        break;
                    case "vecInertiaMultiplier":
                        InertiaMultiplier = ToVec(n);
                        break;
                }
            }

            // Due to mismatched values between SHVDN and handling.meta
            AntiRollBarBiasFront*=2;
            SuspensionBiasFront*=2;
            TractionBiasFront*=2;
            SuspensionCompressionDamping/=10;
            SuspensionReboundDamping/=10;
        }
        public HandlingData(GTA.HandlingData h)
        {
            AntiRollBarBiasFront = h.AntiRollBarBiasFront;
            AntiRollBarForce = h.AntiRollBarForce;
            BrakeForce = h.BrakeForce;
            CamberStiffness=h.CamberStiffness;
            CenterOfMassOffset=h.CenterOfMassOffset;
            ClutchChangeRateScaleDownShift = h.ClutchChangeRateScaleDownShift;
            ClutchChangeRateScaleUpShift = h.ClutchChangeRateScaleUpShift;
            CollisionDamageMultiplier = h.CollisionDamageMultiplier;
            DeformationDamageMultiplier = h.DeformationDamageMultiplier;
            DriveInertia=h.DriveInertia;
            EngineDamageMultiplier = h.EngineDamageMultiplier;
            HandBrakeForce =h.HandBrakeForce;
            InertiaMultiplier=h.InertiaMultiplier;
            InitialDriveForce=h.InitialDriveForce;
            InitialDriveGears=h.InitialDriveGears;
            Mass=h.Mass;
            MonetaryValue=h.MonetaryValue;
            OilVolume=h.OilVolume;
            PercentSubmerged=h.PercentSubmerged;
            PetrolTankVolume=h.PetrolTankVolume;
            RollCenterHeightFront=h.RollCenterHeightFront;
            RollCenterHeightRear=h.RollCenterHeightRear;
            SeatOffsetDistanceX=h.SeatOffsetDistanceX;
            SeatOffsetDistanceY=h.SeatOffsetDistanceY;
            SeatOffsetDistanceZ=h.SeatOffsetDistanceZ;
            SteeringLock=h.SteeringLock;
            SuspensionBiasFront=h.SuspensionBiasFront;
            SuspensionCompressionDamping=h.SuspensionCompressionDamping;
            SuspensionForce=h.SuspensionForce;
            SuspensionLowerLimit=h.SuspensionLowerLimit;
            SuspensionRaise=h.SuspensionRaise;
            SuspensionReboundDamping=h.SuspensionReboundDamping;
            SuspensionUpperLimit=h.SuspensionUpperLimit;
            TractionBiasFront=h.TractionBiasFront;
            TractionCurveMax=h.TractionCurveMax;
            TractionCurveMin=h.TractionCurveMin;
            TractionLossMultiplier=h.TractionLossMultiplier;
            TractionSpringDeltaMax=h.TractionSpringDeltaMax;
            WeaponDamageMultiplier=h.WeaponDamageMultiplier;
        }
        Vector3 ToVec(XmlNode n)
        {
            return new Vector3()
            {
                X=float.Parse(n.Attributes["x"].Value),
                Y=float.Parse(n.Attributes["x"].Value),
                Z=float.Parse(n.Attributes["x"].Value),
            };
        }
        public void ApplyTo(GTA.HandlingData h)
        {
            h.AntiRollBarBiasFront = AntiRollBarBiasFront;
            h.AntiRollBarForce = AntiRollBarForce;
            h.BrakeForce = BrakeForce;
            h.CamberStiffness=CamberStiffness;
            h.CenterOfMassOffset=CenterOfMassOffset;
            h.ClutchChangeRateScaleDownShift = ClutchChangeRateScaleDownShift;
            h.ClutchChangeRateScaleUpShift = ClutchChangeRateScaleUpShift;
            h.CollisionDamageMultiplier = CollisionDamageMultiplier;
            h.DeformationDamageMultiplier = DeformationDamageMultiplier;
            h.DriveInertia=DriveInertia;
            h.EngineDamageMultiplier = EngineDamageMultiplier;
            h.HandBrakeForce = HandBrakeForce;
            h.InertiaMultiplier=InertiaMultiplier;
            h.InitialDriveForce=InitialDriveForce;
            h.InitialDriveGears=InitialDriveGears;
            h.Mass=Mass;
            h.MonetaryValue=MonetaryValue;
            h.OilVolume=OilVolume;
            h.PercentSubmerged=PercentSubmerged;
            h.PetrolTankVolume=PetrolTankVolume;
            h.RollCenterHeightFront=RollCenterHeightFront;
            h.RollCenterHeightRear=RollCenterHeightRear;
            h.SeatOffsetDistanceX=SeatOffsetDistanceX;
            h.SeatOffsetDistanceY=SeatOffsetDistanceY;
            h.SeatOffsetDistanceZ=SeatOffsetDistanceZ;
            // doesn't match
            // h.SteeringLock=SteeringLock;
            h.SuspensionBiasFront=SuspensionBiasFront;
            h.SuspensionCompressionDamping=SuspensionCompressionDamping;
            h.SuspensionForce=SuspensionForce;
            h.SuspensionLowerLimit=SuspensionLowerLimit;
            h.SuspensionRaise=SuspensionRaise;
            h.SuspensionReboundDamping=SuspensionReboundDamping;
            h.SuspensionUpperLimit=SuspensionUpperLimit;
            h.TractionBiasFront=TractionBiasFront;
            h.TractionCurveMax=TractionCurveMax;
            h.TractionCurveMin=TractionCurveMin;
            // doesn't match
            // h.TractionLossMultiplier=TractionLossMultiplier;
            h.TractionSpringDeltaMax=TractionSpringDeltaMax;
            h.WeaponDamageMultiplier=WeaponDamageMultiplier;


        }
        public readonly string Name;
        public readonly float AntiRollBarBiasFront;
        public readonly float AntiRollBarForce;
        public readonly float BrakeForce;
        public readonly float CamberStiffness;
        [JsonIgnore]
        public readonly Vector3 CenterOfMassOffset;
        public readonly float ClutchChangeRateScaleDownShift;
        public readonly float ClutchChangeRateScaleUpShift;
        public readonly float CollisionDamageMultiplier;
        public readonly float DeformationDamageMultiplier;
        public readonly float DriveInertia;
        public readonly float EngineDamageMultiplier;
        public readonly float HandBrakeForce;
        [JsonIgnore]
        public readonly Vector3 InertiaMultiplier;
        public readonly float InitialDriveForce;
        public readonly int InitialDriveGears;
        public readonly float Mass;
        public readonly int MonetaryValue;
        public readonly float OilVolume;
        public readonly float PercentSubmerged;
        public readonly float PetrolTankVolume;
        public readonly float RollCenterHeightFront;
        public readonly float RollCenterHeightRear;
        public readonly float SeatOffsetDistanceX;
        public readonly float SeatOffsetDistanceY;
        public readonly float SeatOffsetDistanceZ;
        public readonly float SteeringLock;
        public readonly float SuspensionBiasFront;
        public readonly float SuspensionCompressionDamping;
        public readonly float SuspensionForce;
        public readonly float SuspensionLowerLimit;
        public readonly float SuspensionRaise;
        public readonly float SuspensionReboundDamping;
        public readonly float SuspensionUpperLimit;
        public readonly float TractionBiasFront;
        public readonly float TractionCurveMax;
        public readonly float TractionCurveMin;
        public readonly float TractionLossMultiplier;
        public readonly float TractionSpringDeltaMax;
        public readonly float WeaponDamageMultiplier;
    }
}
