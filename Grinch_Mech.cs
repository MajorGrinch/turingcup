using System.Linq;
using TuringCup;
using Newtonsoft.Json.Linq;
using UnityEngine;
public class MechTeam : AIBase
{

    public MechTeam(int a_index) : base(a_index) { }

    public override string TeamName
    {
        get
        {
            return "Grinch_Mech";
        }
    }

    public override Character ChooseCharacter
    {
        get
        {
            return Character.Mech;
        }
    }

    private float INF = 10000;

    private float last_x = 0, last_z = 0;
    protected override void Act(JObject state)
    {
        var me = state["me"];
        var barrels = state["barrels"].Children();
        var enemies = state["enemies"].Children();
        var pickup = state["pickups"].Children();

        var tar_pick = pickup.OrderBy(e => Distance(me, e)).Where(e => (int)e["type"] == 0).FirstOrDefault();
        var tar_barrel = barrels.OrderBy(e => Distance(me, e)).FirstOrDefault();
        var tar_hp = pickup.OrderBy(e => Distance(me, e)).Where(e => (int)e["type"] == 1).FirstOrDefault();
        var tar_ene = enemies.OrderBy(e => Distance(me, e)).FirstOrDefault();
        if ((float)me["hp"] <= 90)
        {
            UseSkill(0);
            var x = (float)tar_hp["pos"]["x"];
            var z = (float)tar_hp["pos"]["z"];
            last_x = x; last_z = z;
            Debug.Log("Pick and lack hp");
            Move(last_x, last_z);

        }
        else if (tar_ene != null)
        {
            Debug.Log("enemies\nx: " + tar_ene["pos"]["x"].ToString() + "\tz: " + tar_ene["pos"]["z"].ToString());
            var x = float.Parse(tar_ene["pos"]["x"].ToString());
            var z = float.Parse(tar_ene["pos"]["z"].ToString());
            Move(x, z);
            if (int.Parse(me["skills"][1].ToString()) == 0 && Distance(me, tar_ene) < 20 * 20 && enemies.Count() > 1)
            {
                UseSkill(1, int.Parse(tar_ene["index"].ToString()));
                Move(x, z);
            }
            else if (Distance(me, tar_ene) <= 10)
            {
                System.Random ra = new System.Random(10);
                float rx = float.Parse((ra.Next(0, 1000) / 10.0).ToString());
                float rz = float.Parse((ra.Next(0, 1000) / 10.0).ToString());
                UseSkill(0, x, z);
                Move(rx, rz);
            }
        }
        else if (tar_barrel != null || tar_pick != null)
        {

            if (Distance(me, tar_pick) >= Distance(me, tar_barrel))
            {

                last_x = float.Parse(tar_barrel["pos"]["x"].ToString());
                last_z = float.Parse(tar_barrel["pos"]["z"].ToString());
                UseSkill(0, last_x, last_z);
            }
            else
            {

                if (float.Parse(me["pos"]["x"].ToString()) == last_x && float.Parse(me["pos"]["z"].ToString()) == last_z)
                {
                    last_x = float.Parse(tar_pick["pos"]["x"].ToString());
                    last_z = float.Parse(tar_pick["pos"]["z"].ToString());
                    Debug.Log("Got something");
                    Move(last_x, last_z);
                }
                else
                {
                    Move(last_x, last_z);
                }
            }
        }
        else
        {
            Move(50, 50);
        }
        Debug.Log("Last_x: " + last_x + "\nLast_z: " + last_z);
    }

    private float Distance(JToken a, JToken b)
    {
        if (a == null || b == null)
            return INF;
        float dx = (float)a["pos"]["x"] - (float)b["pos"]["x"];
        float dz = (float)a["pos"]["z"] - (float)b["pos"]["z"];
        return dx * dx + dz * dz;
    }
    private float Distance(JToken a, float x, float z)
    {
        float dx = (float)a["pos"]["x"] - x;
        float dz = (float)a["pos"]["z"] - z;
        return dx * dx + dz * dz;
    }


}
