using System.Collections.Generic;
using UnityEngine;

public enum PartShape
{
    Engine,   // cylinder along the fuselage
    Tank,     // rounded box
    Radar,    // disc facing forward
    Seat,     // box
    Gun,      // thin cylinder along the fuselage
    Bay,      // flat box
}

// One labelled system inside an aircraft, shown in the cutaway view.
// Positions are in metres in the viewer's aircraft frame: x to the right, y up from
// the ground the aircraft stands on, z aft from the tip of the nose.
public class CutawayPart
{
    public string Label;
    public string Detail;
    public PartShape Shape;
    public Vector3 Position;
    public Vector3 Size;
    // Mirrored to the left side as well (paired engines, intakes, wing tanks).
    public bool Mirror;
}

// Facts that are not on the exhibit plaque, used by the quiz badges and the aircraft
// viewer. Keyed by PlaneSO.Name.
public class ExhibitFacts
{
    public string DisplayName;
    public string ShortCode;
    public float LengthMetres;
    public string Engines;
    public string Gun;
    public int Crew;
    public string Trivia;
    public List<CutawayPart> Parts = new();

    public static ExhibitFacts For(string planeName)
    {
        return planeName != null && All.TryGetValue(planeName, out ExhibitFacts facts) ? facts : null;
    }

    static CutawayPart Part(string label, PartShape shape, Vector3 position, Vector3 size, string detail, bool mirror = false)
    {
        return new CutawayPart { Label = label, Shape = shape, Position = position, Size = size, Detail = detail, Mirror = mirror };
    }

    public static readonly Dictionary<string, ExhibitFacts> All = new()
    {
        ["A-10 Thunderbolt II"] = new ExhibitFacts
        {
            DisplayName = "A-10 Thunderbolt II",
            ShortCode = "A-10",
            LengthMetres = 16.26f,
            Engines = "2 × General Electric TF34 turbofans",
            Gun = "GAU-8/A Avenger 30 mm rotary cannon",
            Crew = 1,
            Trivia = "Designed around a huge seven-barrel 30 mm cannon, with a titanium \"bathtub\" of armour around the pilot.",
            Parts =
            {
                Part("GAU-8/A Avenger cannon", PartShape.Gun, new Vector3(0f, 2.25f, 2.4f), new Vector3(0.4f, 0.4f, 4.2f),
                    "A seven-barrel 30 mm rotary cannon almost 6 m long with its feed system, firing about 3,900 rounds a minute."),
                Part("Ammunition drum", PartShape.Tank, new Vector3(0f, 2.55f, 5.4f), new Vector3(0.95f, 0.95f, 1.9f),
                    "The drum behind the gun holds up to 1,174 rounds of 30 mm ammunition."),
                Part("Titanium \"bathtub\"", PartShape.Seat, new Vector3(0f, 3.05f, 3.9f), new Vector3(0.95f, 0.95f, 1.4f),
                    "The pilot sits in a tub of titanium armour built to withstand hits from ground fire."),
                Part("Fuel tanks", PartShape.Tank, new Vector3(0f, 2.9f, 8.6f), new Vector3(1.1f, 1.0f, 3.4f),
                    "Self-sealing fuel cells in the fuselage and wing centre section, protected by foam."),
                Part("TF34 turbofans", PartShape.Engine, new Vector3(1.45f, 3.95f, 11.6f), new Vector3(1.2f, 1.2f, 3.4f),
                    "Two General Electric TF34 engines in pods high on the rear fuselage, shielded by the wings and tail.", mirror: true),
            },
        },
        ["B-2 Spirit"] = new ExhibitFacts
        {
            DisplayName = "B-2 Spirit",
            ShortCode = "B-2",
            LengthMetres = 20.9f,
            Engines = "4 × General Electric F118 turbofans",
            Gun = "None — all weapons are carried internally",
            Crew = 2,
            Trivia = "A flying wing with no vertical tail that can fly about 6,000 nautical miles without refuelling.",
            Parts =
            {
                Part("Crew station", PartShape.Seat, new Vector3(0.55f, 6.9f, 3.8f), new Vector3(0.7f, 0.9f, 1.2f),
                    "Two crew sit side by side: the pilot on the left and the mission commander on the right.", mirror: true),
                Part("AN/APQ-181 radar", PartShape.Radar, new Vector3(3.0f, 5.4f, 3.2f), new Vector3(1.1f, 0.5f, 0.15f),
                    "Radar antennas under the leading edge either side of the cockpit, used for navigation and targeting."),
                Part("Weapons bays", PartShape.Bay, new Vector3(1.4f, 5.4f, 9.0f), new Vector3(1.8f, 1.1f, 5.0f),
                    "Two internal bays with rotary launchers carry up to 40,000 lb of weapons, hidden from radar.", mirror: true),
                Part("F118 engines (inboard)", PartShape.Engine, new Vector3(3.3f, 6.0f, 10.8f), new Vector3(1.1f, 1.0f, 3.2f),
                    "Four General Electric F118 turbofans are buried inside the wing, with their exhausts on top to hide heat.", mirror: true),
                Part("F118 engines (outboard)", PartShape.Engine, new Vector3(4.7f, 6.0f, 11.2f), new Vector3(1.1f, 1.0f, 3.2f),
                    "The engines have no afterburners; the B-2 cruises just below the speed of sound.", mirror: true),
                Part("Wing fuel tanks", PartShape.Tank, new Vector3(8.0f, 5.9f, 13.5f), new Vector3(2.4f, 0.6f, 3.4f),
                    "Fuel is carried inside the wing, giving the bomber its intercontinental range.", mirror: true),
            },
        },
        ["Eurofighter Typhoon"] = new ExhibitFacts
        {
            DisplayName = "Eurofighter Typhoon",
            ShortCode = "EF",
            LengthMetres = 15.96f,
            Engines = "2 × Eurojet EJ200 turbofans",
            Gun = "Mauser BK-27 27 mm revolver cannon",
            Crew = 1,
            Trivia = "A canard-delta fighter built by a consortium from the UK, Germany, Italy and Spain.",
            Parts =
            {
                Part("CAPTOR radar", PartShape.Radar, new Vector3(0f, 2.55f, 0.9f), new Vector3(0.6f, 0.6f, 0.12f),
                    "The CAPTOR radar in the nose finds and tracks targets in the air and on the ground."),
                Part("Ejection seat", PartShape.Seat, new Vector3(0f, 3.0f, 4.6f), new Vector3(0.6f, 1.0f, 0.9f),
                    "A Martin-Baker Mk16A ejection seat under the bubble canopy."),
                Part("BK-27 cannon", PartShape.Gun, new Vector3(0.9f, 2.1f, 6.8f), new Vector3(0.22f, 0.22f, 2.3f),
                    "A Mauser 27 mm revolver cannon in the starboard wing root."),
                Part("Fuselage fuel tanks", PartShape.Tank, new Vector3(0f, 3.0f, 7.8f), new Vector3(1.2f, 0.8f, 3.0f),
                    "Fuel tanks run along the spine between the cockpit and the engines."),
                Part("EJ200 turbofans", PartShape.Engine, new Vector3(0.55f, 2.35f, 12.2f), new Vector3(0.75f, 0.75f, 4.0f),
                    "Two Eurojet EJ200 afterburning engines, each about 4 m long.", mirror: true),
            },
        },
        ["F-14 Tomcat"] = new ExhibitFacts
        {
            DisplayName = "F-14 Tomcat",
            ShortCode = "F-14",
            LengthMetres = 19.1f,
            Engines = "2 × General Electric F110-GE-400 turbofans",
            Gun = "M61A1 Vulcan 20 mm rotary cannon",
            Crew = 2,
            Trivia = "Its variable-sweep wings move automatically in flight, and it starred in the 1986 film Top Gun.",
            Parts =
            {
                Part("AN/APG-71 radar", PartShape.Radar, new Vector3(0f, 1.95f, 1.1f), new Vector3(0.9f, 0.9f, 0.15f),
                    "The F-14D's AN/APG-71 radar can track many targets at long range."),
                Part("Pilot's seat", PartShape.Seat, new Vector3(0f, 2.9f, 4.2f), new Vector3(0.6f, 0.9f, 0.9f),
                    "The pilot sits in the front cockpit."),
                Part("RIO's seat", PartShape.Seat, new Vector3(0f, 3.0f, 5.9f), new Vector3(0.6f, 0.9f, 0.9f),
                    "The Radar Intercept Officer in the back seat runs the radar and weapons."),
                Part("M61A1 Vulcan cannon", PartShape.Gun, new Vector3(-0.55f, 1.75f, 4.2f), new Vector3(0.25f, 0.25f, 1.9f),
                    "A six-barrel 20 mm rotary cannon in the lower left side of the nose."),
                Part("Wing sweep pivots", PartShape.Bay, new Vector3(3.2f, 2.9f, 10.2f), new Vector3(0.5f, 0.3f, 0.5f),
                    "The wings pivot here, sweeping from 20 to 68 degrees as the aircraft speeds up.", mirror: true),
                Part("Fuselage fuel tanks", PartShape.Tank, new Vector3(0f, 2.9f, 9.6f), new Vector3(1.4f, 0.7f, 3.5f),
                    "Main fuel cells sit in the fuselage between the wing pivots."),
                Part("F110-GE-400 turbofans", PartShape.Engine, new Vector3(1.45f, 2.55f, 14.8f), new Vector3(1.0f, 1.0f, 4.6f),
                    "Two General Electric F110 engines in widely spaced nacelles.", mirror: true),
            },
        },
        ["F-16 Fighting Falcon"] = new ExhibitFacts
        {
            DisplayName = "F-16 Fighting Falcon",
            ShortCode = "F-16",
            LengthMetres = 15.06f,
            Engines = "1 × General Electric F110 or Pratt & Whitney F100 turbofan",
            Gun = "M61A1 Vulcan 20 mm rotary cannon",
            Crew = 1,
            Trivia = "Its pilot flies with a side-mounted control stick from a seat reclined 30 degrees, under a frameless bubble canopy.",
            Parts =
            {
                Part("AN/APG-68 radar", PartShape.Radar, new Vector3(0f, 1.8f, 0.9f), new Vector3(0.5f, 0.5f, 0.12f),
                    "The AN/APG-68 radar (newer jets carry the APG-83 AESA) sits behind the nose cone."),
                Part("Reclined seat", PartShape.Seat, new Vector3(0f, 2.35f, 4.9f), new Vector3(0.6f, 0.9f, 0.9f),
                    "The ACES II seat is reclined 30 degrees to help the pilot withstand high g-forces."),
                Part("M61A1 Vulcan cannon", PartShape.Gun, new Vector3(-0.7f, 2.3f, 6.3f), new Vector3(0.25f, 0.25f, 1.9f),
                    "A 20 mm rotary cannon in the left wing root, fed from a drum behind the cockpit."),
                Part("Fuel tanks", PartShape.Tank, new Vector3(0f, 2.4f, 7.9f), new Vector3(1.1f, 0.7f, 3.0f),
                    "Fuel fills the fuselage and wings; more can be carried in external tanks."),
                Part("F110 / F100 turbofan", PartShape.Engine, new Vector3(0f, 1.95f, 12.2f), new Vector3(1.0f, 1.0f, 4.8f),
                    "A single afterburning engine, fed by the intake under the cockpit."),
            },
        },
        ["F-22 Raptor"] = new ExhibitFacts
        {
            DisplayName = "F-22 Raptor",
            ShortCode = "F-22",
            LengthMetres = 18.92f,
            Engines = "2 × Pratt & Whitney F119 turbofans",
            Gun = "M61A2 Vulcan 20 mm rotary cannon",
            Crew = 1,
            Trivia = "Thrust-vectoring nozzles and supercruise let it fly faster than sound without using afterburner.",
            Parts =
            {
                Part("AN/APG-77 radar", PartShape.Radar, new Vector3(0f, 2.0f, 1.0f), new Vector3(0.8f, 0.8f, 0.12f),
                    "An AESA radar whose beam is steered electronically, with no moving dish."),
                Part("Ejection seat", PartShape.Seat, new Vector3(0f, 2.6f, 4.4f), new Vector3(0.6f, 0.9f, 0.9f),
                    "An ACES II ejection seat under a one-piece canopy."),
                Part("Side weapons bays", PartShape.Bay, new Vector3(1.3f, 1.6f, 6.2f), new Vector3(0.3f, 0.4f, 2.2f),
                    "Each side of the intakes has a small bay for a short-range missile.", mirror: true),
                Part("M61A2 Vulcan cannon", PartShape.Gun, new Vector3(1.2f, 2.4f, 7.8f), new Vector3(0.25f, 0.25f, 1.9f),
                    "A 20 mm rotary cannon hidden behind a door in the starboard wing root."),
                Part("Main weapons bay", PartShape.Bay, new Vector3(0f, 1.35f, 8.6f), new Vector3(1.3f, 0.45f, 4.3f),
                    "The main bay under the fuselage carries missiles or bombs inside, keeping the jet stealthy."),
                Part("Fuel tanks", PartShape.Tank, new Vector3(0f, 2.4f, 10.6f), new Vector3(1.4f, 0.6f, 3.0f),
                    "Internal fuel tanks fill the fuselage and wings."),
                Part("F119 turbofans", PartShape.Engine, new Vector3(0.6f, 1.95f, 15.2f), new Vector3(0.95f, 0.95f, 5.2f),
                    "Two Pratt & Whitney F119 engines with thrust-vectoring nozzles that swivel up and down.", mirror: true),
            },
        },
        ["F-35 Lightning"] = new ExhibitFacts
        {
            DisplayName = "F-35 Lightning II",
            ShortCode = "F-35",
            LengthMetres = 15.7f,
            Engines = "1 × Pratt & Whitney F135 turbofan",
            Gun = "GAU-22/A 25 mm rotary cannon",
            Crew = 1,
            Trivia = "Built in three versions, including one that can take off from short runways and land vertically.",
            Parts =
            {
                Part("AN/APG-81 radar", PartShape.Radar, new Vector3(0f, 2.4f, 0.8f), new Vector3(0.6f, 0.6f, 0.12f),
                    "An AESA radar, one of several sensors fused into a single picture for the pilot."),
                Part("Ejection seat", PartShape.Seat, new Vector3(0f, 2.9f, 3.7f), new Vector3(0.6f, 0.9f, 0.9f),
                    "A Martin-Baker US16E ejection seat."),
                Part("GAU-22/A cannon", PartShape.Gun, new Vector3(-0.9f, 3.0f, 5.5f), new Vector3(0.25f, 0.25f, 1.6f),
                    "The F-35A carries a four-barrel 25 mm cannon internally, above the left intake."),
                Part("Internal weapons bays", PartShape.Bay, new Vector3(0.8f, 1.75f, 7.0f), new Vector3(0.6f, 0.5f, 4.2f),
                    "Two bays under the fuselage carry weapons inside to keep the aircraft stealthy.", mirror: true),
                Part("Fuel tanks", PartShape.Tank, new Vector3(0f, 3.0f, 8.6f), new Vector3(1.6f, 0.6f, 3.0f),
                    "Fuel wraps around the engine and fills the wings."),
                Part("F135 turbofan", PartShape.Engine, new Vector3(0f, 2.3f, 12.0f), new Vector3(1.15f, 1.15f, 5.6f),
                    "A single Pratt & Whitney F135, one of the most powerful fighter engines ever built."),
            },
        },
        ["F/A-18 Hornet"] = new ExhibitFacts
        {
            DisplayName = "F/A-18 Hornet",
            ShortCode = "F/A-18",
            LengthMetres = 17.1f,
            Engines = "2 × General Electric F404 turbofans",
            Gun = "M61A1 Vulcan 20 mm rotary cannon",
            Crew = 1,
            Trivia = "A carrier-based fighter and attack jet that the US Navy's Blue Angels flew for more than 30 years.",
            Parts =
            {
                Part("AN/APG-65 radar", PartShape.Radar, new Vector3(0f, 1.85f, 0.9f), new Vector3(0.6f, 0.6f, 0.12f),
                    "The AN/APG-65 radar (APG-73 on later jets) sits behind the nose cone."),
                Part("M61A1 Vulcan cannon", PartShape.Gun, new Vector3(0f, 2.35f, 1.9f), new Vector3(0.25f, 0.25f, 1.9f),
                    "The 20 mm rotary cannon is mounted in the top of the nose, above the radar."),
                Part("Ejection seat", PartShape.Seat, new Vector3(0f, 2.6f, 5.6f), new Vector3(0.6f, 0.9f, 0.9f),
                    "A Martin-Baker ejection seat."),
                Part("Fuel tanks", PartShape.Tank, new Vector3(0f, 2.7f, 8.6f), new Vector3(1.3f, 0.7f, 3.5f),
                    "Fuel cells run along the fuselage above the engines' intake ducts."),
                Part("F404 turbofans", PartShape.Engine, new Vector3(0.55f, 2.2f, 13.8f), new Vector3(0.85f, 0.85f, 4.0f),
                    "Two General Electric F404 engines, known for their reliability at sea.", mirror: true),
            },
        },
        ["Dassault Rafale"] = new ExhibitFacts
        {
            DisplayName = "Dassault Rafale",
            ShortCode = "RAF",
            LengthMetres = 15.27f,
            Engines = "2 × Safran M88 turbofans",
            Gun = "GIAT 30M791 30 mm revolver cannon",
            Crew = 1,
            Trivia = "A French twin-engine fighter that flies from land bases and from the aircraft carrier Charles de Gaulle.",
            Parts =
            {
                Part("RBE2 radar", PartShape.Radar, new Vector3(0f, 2.8f, 0.9f), new Vector3(0.55f, 0.55f, 0.12f),
                    "The RBE2 radar; newer Rafales have an electronically scanned version."),
                Part("Ejection seat", PartShape.Seat, new Vector3(0f, 3.3f, 4.7f), new Vector3(0.6f, 0.9f, 0.9f),
                    "A Martin-Baker Mk16F seat, reclined to help with g-forces."),
                Part("30M791 cannon", PartShape.Gun, new Vector3(0.9f, 2.4f, 6.5f), new Vector3(0.25f, 0.25f, 2.0f),
                    "A 30 mm revolver cannon on the starboard side of the fuselage."),
                Part("Fuel tanks", PartShape.Tank, new Vector3(0f, 3.2f, 8.0f), new Vector3(1.3f, 0.7f, 3.0f),
                    "Internal fuel in the fuselage and wings, with a refuelling probe beside the cockpit."),
                Part("M88 turbofans", PartShape.Engine, new Vector3(0.45f, 2.55f, 12.3f), new Vector3(0.7f, 0.7f, 3.5f),
                    "Two Safran M88 engines, each about 3.5 m long.", mirror: true),
            },
        },
    };
}
