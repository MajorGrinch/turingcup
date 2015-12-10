using System.Linq;
using TuringCup;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
public class Grinch : AIBase
{

    public Grinch(int a_index) : base(a_index) { }

    public override string TeamName
    {
        get
        {
            return "Grinch";
        }
    }

    public override Character ChooseCharacter
    {
        get
        {
            return Character.Tyor;
        }
    }

    private float shootRange = 16;
    private float INF = 10000;
    private float last_x = 0, last_z = 0;
    private int use_thor = 0;
    private float birth_x, birth_z;
    private bool hit_miss_flag = false;
    protected override void Act(JObject state)
    {
        float time = float.Parse(state["time"].ToString());
        var me = state["me"];
        int me_index = (int)me["index"];
        var barrels = state["barrels"].Children();
        var enemies = state["enemies"].Children();
        var pickup = state["pickups"].Children();
        var misc = state["misc"].Children();
        float cd_1 = float.Parse(me["cd"][1].ToString());
        var me_pos_x = float.Parse(me["pos"]["x"].ToString());
        var me_pos_z = float.Parse(me["pos"]["z"].ToString());

        var tar_hp = pickup.OrderBy(e => Distance(me, e)).Where(e => (int)e["type"] == 1).FirstOrDefault();

        var meteorite = misc.OrderBy(e => Distance(me, e)).Where(e => (string)e["type"] == "meteorite").FirstOrDefault();
        var missile = misc.OrderBy(e => Distance(me, e)).Where(e => (string)e["type"] == "missile" && (int)e["target"] == me_index).FirstOrDefault();
        var stray_miss = misc.OrderBy(e => Distance(me, e)).Where(e => (string)e["type"] == "missile" && (int)e["target"] != me_index).FirstOrDefault();
        var snowstorm = misc.OrderBy(e => Distance(me, e)).Where(e => (string)e["type"] == "snowstorm").FirstOrDefault();

        if (time <= 0.1)  //get birth position
        {
            birth_x = float.Parse(me["pos"]["x"].ToString());
            birth_z = float.Parse(me["pos"]["z"].ToString());
        }
        if ((float)me["hp"] <= 60 && tar_hp != null)  //lack hp and pick hp if has
        {
            Pick_Hp(me, tar_hp);
            goto HAHA;
        }
        if (Distance(me, meteorite) <= 10 * 10)    // escape from the meteorite
        {
            Escape_Meteorite(meteorite, me);
            //goto HAHA;
        }

        if (missile == null)
            hit_miss_flag = false;

        if (missile != null)    // escape from the missile if the distance between me and the missile is less than 12
        {
            Escape_Missile(missile, me);
            goto HAHA;
        }
        if (Distance(me, snowstorm) <= 10.5 * 10.5)   // escape from the snowstorm
        {
            Escape_Snowstorm(snowstorm, me);
            goto HAHA;
        }
        if (stray_miss != null)       // escape from the stray missile to avoid unnecessary hurt
        {
            Escape_Stray(stray_miss, me, enemies);
            goto HAHA;
        }
        /*if ((me_pos_x != last_x || me_pos_z != last_z) && last_x != 0 && last_z != 0)
            goto HAHA;
         */
        if (use_thor == 0 && time > 0.1)  //Initiate befroe hit the first enemy with skill 1 which means the fight has enter into a new stage
        {
            Init(me, enemies, barrels, pickup);
        }
        else if (time <= 70)            // enter into fierce stage until last 30 seconds
        {
            Fierce(me, enemies, misc);
        }
        else                     //In the last 30 seconds, the main task is to got others to gain score
        {
            End(me, enemies, misc);
        }
    HAHA: ;
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

    private bool Check_Barrel(JToken barrel)
    {
        float Axis_vec_x = 50 - birth_x;
        float Axis_vec_z = 50 - birth_z;
        float Axis_vec_mod = (float)System.Math.Sqrt(Axis_vec_x * Axis_vec_x + Axis_vec_z * Axis_vec_z);
        float bar_x = float.Parse(barrel["pos"]["x"].ToString());
        float bar_z = float.Parse(barrel["pos"]["z"].ToString());
        float bar_vec_x = bar_x - birth_x;
        float bar_vec_z = bar_z - birth_z;
        float bar_vec_mod = (float)System.Math.Sqrt(bar_vec_x * bar_vec_x + bar_vec_z * bar_vec_z);
        if (bar_vec_mod >= 63.639)
            return false;
        float product = Axis_vec_z * bar_vec_x - Axis_vec_x * bar_vec_z;
        float sin_theta = System.Math.Abs(product) / (bar_vec_mod * Axis_vec_mod);
        float dis = bar_vec_mod * sin_theta;
        if (dis <= 7)
            return true;
        else
            return false;
    }

    private void Escape_Meteorite(JToken meteorite, JToken me)
    {
        float met_x = float.Parse(meteorite["pos"]["x"].ToString());
        float met_z = float.Parse(meteorite["pos"]["z"].ToString());
        float me_x = float.Parse(me["pos"]["x"].ToString());
        float me_z = float.Parse(me["pos"]["z"].ToString());
        float vec_x = (met_x - me_x) * 10;
        float vec_z = (met_z - me_z) * 10;
        if (vec_x == 0)
        {
            System.Random ra = new System.Random(10);
            vec_x = float.Parse((ra.Next(100, 1000) / 100.0).ToString());
            vec_z = float.Parse((ra.Next(100, 1000) / 100.0).ToString());
        }
        if (int.Parse(me["skills"][0].ToString()) == 0)
            UseSkill(0);
        Move(me_x - 10 * vec_x, me_z - 10 * vec_z);
        Debug.Log("Run to " + (me_x - vec_x).ToString() + " compare to " + me_x.ToString() + " to escape meteorite");
    }

    private void Escape_Missile(JToken missile, JToken me)
    {
        float cd_1 = float.Parse(me["cd"][1].ToString());
        int mis_index = int.Parse(missile["index"].ToString());
        float mis_x = float.Parse(missile["pos"]["x"].ToString());
        float mis_z = float.Parse(missile["pos"]["z"].ToString());
        float me_x = float.Parse(me["pos"]["x"].ToString());
        float me_z = float.Parse(me["pos"]["z"].ToString());
        float me_vel_x = float.Parse(me["vel"]["x"].ToString());
        float me_vel_z = float.Parse(me["vel"]["z"].ToString());
        float me_speed = (float)System.Math.Sqrt(me_vel_x * me_vel_x + me_vel_z * me_vel_z);
        float mis_speed = float.Parse(missile["speed"].ToString());
        float dis = Distance(missile, me);
        double x = (double)System.Math.Sqrt(dis);
        double t;
        if (mis_speed < 10)
        {

            double b = mis_speed - me_speed;
            double c = -x + 8;
            t = -b + System.Math.Sqrt(b * b - c);
            t *= 2;
        }
        else
        {
            t = (x - 8) / (10 - me_speed);
        }

        if (t < 0.3 && int.Parse(me["skills"][1].ToString()) == 0)
        {
            UseSkill(1, mis_index);
            hit_miss_flag = true;
            use_thor++;
        }
        else if (t < cd_1 && !hit_miss_flag)      //unable to escape
        {
            Suicide(missile, me);
        }
        else if (x < 12)
        {
            if (int.Parse(me["skills"][0].ToString()) == 0)
                UseSkill(0);
            float vec_x = mis_x - me_x;
            float vec_z = mis_z - me_z;
            Move(me_x - vec_x, me_z - vec_z);
        }
    }

    private void Escape_Snowstorm(JToken snowstorm, JToken me)
    {
        float met_x = float.Parse(snowstorm["pos"]["x"].ToString());
        float met_z = float.Parse(snowstorm["pos"]["z"].ToString());
        float me_x = float.Parse(me["pos"]["x"].ToString());
        float me_z = float.Parse(me["pos"]["z"].ToString());
        float vec_x = met_x - me_x;
        float vec_z = met_z - me_z;
        if (int.Parse(me["skills"][0].ToString()) == 0)
            UseSkill(0);
        Move(me_x - 20*vec_x, me_z - 20*vec_z);
    }

    private void Escape_Stray(JToken stray_miss, JToken me, JEnumerable<JToken> ene)
    {
        int me_index = (int)me["index"];
        int miss_tar_index = (int)stray_miss["index"];
        var miss_target = ene.Where(e => (int)e["index"] == miss_tar_index).FirstOrDefault();

        float stray_mis_speed = float.Parse(stray_miss["speed"].ToString());
        float stray_mis_x = float.Parse(stray_miss["pos"]["x"].ToString());
        float stray_mis_z = float.Parse(stray_miss["pos"]["z"].ToString());
        float me_x = float.Parse(me["pos"]["x"].ToString());
        float me_z = float.Parse(me["pos"]["z"].ToString());
        float miss_tar_x = float.Parse(miss_target["pos"]["x"].ToString());
        float miss_tar_z = float.Parse(miss_target["pos"]["z"].ToString());

        if (stray_mis_speed == 0 && Distance(me, stray_miss) <= 5 *5)
        {
            float vec_x = stray_mis_x - me_x;
            float vec_z = stray_mis_z - me_z;
            if (int.Parse(me["skills"][0].ToString()) == 0)
                UseSkill(0);
            Move(me_x - 30*vec_x, me_z - 30*vec_z);
        }

        if (Distance(stray_miss, miss_target) <= 5 * 5)
        {
            float vec_0_x = stray_mis_x - miss_tar_x;
            float vec_0_z = stray_mis_z - miss_tar_z;
            float vec_1_x = stray_mis_x - me_x;
            float vec_1_z = stray_mis_z - me_z;
            float vec_x = -vec_0_z;
            float vec_z = vec_0_x;
            float res = vec_x * vec_1_x + vec_z * vec_1_z;
            if (res < 0)
            {
                vec_x = -vec_x;
                vec_z = -vec_z;
            }
            if (int.Parse(me["skills"][0].ToString()) == 0)
                UseSkill(0);
            Move(me_x - 30*vec_x, me_z - 30*vec_z);
        }
    }

    private void Pick_Hp(JToken me, JToken hp)
    {    
        if (int.Parse(me["skills"][0].ToString()) == 0)
            UseSkill(0);
        UseSkill(0);
        var x = float.Parse(hp["pos"]["x"].ToString());
        var z = float.Parse(hp["pos"]["z"].ToString());
        last_x = x; last_z = z;
        Move( last_x,  last_z);
        if (Distance(me, last_x, last_z) <= 0.5 )
        {
            last_x = 0;
            last_z = 0;
        }

    }

    private void Fierce(JToken me, JEnumerable<JToken> enemies, JEnumerable<JToken> misc)
    {

        var score_ene = enemies.OrderBy(e => (float)e["score"]).LastOrDefault();
        var near_ene = enemies.OrderBy(e => Distance(e, me)).FirstOrDefault();
        var direct_hit = enemies.Where(e => (float)e["hp"] <= 36).FirstOrDefault();
        int skill_0_state = (int)me["skills"][0];
        int skill_1_state = (int)me["skills"][1];

        if (direct_hit != null)   // the use of skill 1
        {
            int direct_index = (int)direct_hit["index"];
            var x = float.Parse(direct_hit["pos"]["x"].ToString());
            var z = float.Parse(direct_hit["pos"]["z"].ToString());
            if (skill_1_state == 0 && Distance(me, direct_hit) <= shootRange * shootRange)
            {
                UseSkill(1, direct_index);
            }
            Move(x, z);
        }

        else if (Distance(score_ene, me) <= shootRange * shootRange)
        {
            int score_ene_index = (int)score_ene["index"];
            if (skill_1_state == 0)
                UseSkill(1, score_ene_index);
        }

        if (near_ene != null)
        {
            var x = float.Parse(near_ene["pos"]["x"].ToString());
            var z = float.Parse(near_ene["pos"]["z"].ToString());
            if (Distance(me, near_ene) <= 8 * 8 && skill_0_state == 0)
                UseSkill(0);
            Move(x, z);
        }
        else
        {
            Move(50, 50);
        }
        Debug.Log("Execute the fierce");
    }

    private void Init(JToken me, JEnumerable<JToken> enemies, JEnumerable<JToken> barrels, JEnumerable<JToken> pickups)
    {
        var tar_barrel = barrels.OrderBy(e => Distance(me, e)).Where(e => Check_Barrel(e) == true).FirstOrDefault();
        var tar_ene = enemies.OrderBy(e => Distance(me, e)).FirstOrDefault();
        var tar_pick = pickups.OrderBy(e => Distance(me, e)).Where(e => (int)e["type"] == 0).FirstOrDefault();
        int skill_0_state = (int)me["skills"][0];
        int skill_1_state = (int)me["skills"][1];
        if (Distance(me, tar_ene) <= shootRange * shootRange && skill_1_state == 0)
        {
            int target_index = (int)tar_ene["index"];
            UseSkill(1, target_index);
            use_thor++;
        }

        if (tar_pick != null)
        {
            float x = float.Parse(tar_pick["pos"]["x"].ToString());
            float z = float.Parse(tar_pick["pos"]["z"].ToString());
            Move(x, z);
        }
        else if (tar_barrel != null)
        {
            float x = float.Parse(tar_barrel["pos"]["x"].ToString());
            float z = float.Parse(tar_barrel["pos"]["z"].ToString());
            if (Distance(me, tar_barrel) < 16 * 16 && skill_0_state == 0)
                UseSkill(0);
            Move(x, z);
        }
        else
        {
            Move(50, 50);
        }
        Debug.Log("Execute the Init");
    }

    private void End(JToken me, JEnumerable<JToken> enemies, JEnumerable<JToken> misc)
    {
        var weak_ene = enemies.OrderBy(e => (float)e["hp"]).FirstOrDefault();
        int weak_ene_index = (int)weak_ene["index"];
        int skill_0_state = (int)me["skills"][0];
        int skill_1_state = (int)me["skills"][1];
        if (weak_ene != null)
        {
            var x = float.Parse(weak_ene["pos"]["x"].ToString());
            var z = float.Parse(weak_ene["pos"]["z"].ToString());
            if (skill_0_state == 0 && Distance(me, weak_ene) <= 8 * 8)
                UseSkill(0);
            if (skill_1_state == 0 && Distance(me, weak_ene) <= shootRange * shootRange)
            {
                UseSkill(1, weak_ene_index);
            }
            Move(x, z);
        }
        else
        {
            Move(50, 50);
        }
        Debug.Log("Execute the End");
    }

    private void Avoid(JToken me, JEnumerable<JToken> target)
    {
        
        float me_vel_x = float.Parse(me["vel"]["x"].ToString());
        float me_vel_z = float.Parse(me["vel"]["z"].ToString());
        float tar_x = float.Parse(target["pos"]["x"].ToString());
        float tar_z = float.Parse(target["pos"]["z"].ToString());
        float me_x = float.Parse(me["pos"]["x"].ToString());
        float me_z = float.Parse(me["pos"]["z"].ToString());
        float k = me_vel_z / me_vel_x;
        float divisor = (float)System.Math.Sqrt(k * k + 1);
        float dividend = k * tar_x - tar_z + me_z - k * me_x;
        float dis = dividend / divisor;
        if (dis <= 10)
        {
            float vec_me_tar_x = tar_x - me_x;
            float vec_me_tar_z = tar_z - me_z;
            float res = vec_me_tar_x * me_vel_x + vec_me_tar_z * me_vel_z;
            /*if (res > 0 && Distance(me,target) <= 15 )
            {
                ;
            }*/
        }
    }

    private void Suicide(JToken missile, JToken me)
    {
        var x = float.Parse(missile["pos"]["x"].ToString());
        var z = float.Parse(missile["pos"]["z"].ToString());
        if ((int)me["skills"][0] == 0)
            UseSkill(0);
        Move(x, z);
    }

}
