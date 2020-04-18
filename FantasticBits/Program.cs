using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Grab Snaffles and try to throw them through the opponent's goal!
 * Move towards a Snaffle and use your team id to determine where you need to throw it.
 **/
public static class Rules
{
    public static int FieldWidth => 16000;
    public static int FieldHeight => 7501;

    public static int GoalBottom => 5750;
    public static int GoalCenter => 3750;
    public static int GoalTop => 1750;

    public static int ObliviateCost => 5;
    public static int PetrificusCost => 10;
    public static int AccioCost => 15;
    public static int FlipendoCost => 20;
}

public class Game
{
    public int MyScore { get; set; }
    public int MyMagic { get; set; }
    public int OpponentScore { get; set; }
    public int OpponentMagic { get; set; }
}

public static class Utils
{
    public static double GetDistance(int X1, int Y1, int X2, int Y2)
    {
        return Math.Sqrt(Math.Pow(X1 - X2, 2) + Math.Pow(Y1 - Y2, 2));
    }

    public static bool IsInsideTriangle(int X, int Y, int AX, int AY, int BX, int BY, int CX, int CY)
    {
        var d1 = MagicCrossProduct(X, Y, AX, AY, CX, CY);
        var d2 = MagicCrossProduct(X, Y, BX, BY, AX, AY);
        var d3 = MagicCrossProduct(X, Y, CX, CY, BX, BY);

        return d1 < 0 && d2 < 0 && d3 < 0;
    }

    public static int MagicCrossProduct(int X, int Y, int X1, int Y1, int X2, int Y2)
    {
        return (X - X1) * (Y2 - Y1) - (Y - Y1) * (X2 - X1);
    }
}

class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        int myTeamId =
            int.Parse(Console
                .ReadLine()); // if 0 you need to score on the right of the map, if 1 you need to score on the left
        var game = new Game();
        var master = new Forwarder() {IsMasterPlayer = true, Game = game};
        var slave = new Forwarder() {IsMasterPlayer = false, Companion = master, Game = game};
        master.Companion = slave;

        // game loop
        while (true)
        {
            List<GameObject> snaffles = new List<GameObject>();
            List<GameObject> bludgers = new List<GameObject>();

            inputs = Console.ReadLine().Split(' ');
            int myScore = int.Parse(inputs[0]);
            int myMagic = int.Parse(inputs[1]);
            inputs = Console.ReadLine().Split(' ');
            int opponentScore = int.Parse(inputs[0]);
            int opponentMagic = int.Parse(inputs[1]);
            game.MyMagic = myMagic;
            game.MyScore = myScore;
            game.OpponentMagic = opponentMagic;
            game.OpponentScore = opponentScore;
            int entities = int.Parse(Console.ReadLine()); // number of entities still in game
            for (int i = 0; i < entities; i++)
            {
                snaffles = new List<GameObject>();
                bludgers = new List<GameObject>();
                inputs = Console.ReadLine().Split(' ');
                int entityId = int.Parse(inputs[0]); // entity identifier
                string
                    entityType =
                        inputs[1]; // "WIZARD", "OPPONENT_WIZARD" or "SNAFFLE" (or "BLUDGER" after first league)
                int x = int.Parse(inputs[2]); // position
                int y = int.Parse(inputs[3]); // position
                int vx = int.Parse(inputs[4]); // velocity
                int vy = int.Parse(inputs[5]); // velocity
                int state = int.Parse(inputs[6]); // 1 if the wizard is holding a Snaffle, 0 otherwise

                switch (entityType)
                {
                    case "WIZARD":
                        if (entityId % 2 == 0)
                        {
                            master.X = x;
                            master.Y = y;
                            master.VX = vx;
                            master.VY = vy;
                            master.Holding = state == 1;
                        }
                        else
                        {
                            slave.X = x;
                            slave.Y = y;
                            slave.VX = vx;
                            slave.VY = vy;
                            slave.Holding = state == 1;
                        }

                        break;
                    case "SNAFFLE":
                        snaffles.Add(
                            new GameObject()
                            {
                                Id = entityId,
                                X = x,
                                Y = y,
                                VX = vx,
                                VY = vy
                            });
                        break;
                    case "BLUDGER":
                        bludgers.Add(
                            new GameObject()
                            {
                                Id = entityId,
                                X = x,
                                Y = y,
                                VX = vx,
                                VY = vy
                            });
                        break;
                }
            }

            master.DoSomething(snaffles, bludgers, myTeamId);
            slave.DoSomething(snaffles, bludgers, myTeamId);
        }
    }
}

public class GameObject
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int VX { get; set; }
    public int VY { get; set; }

    public Game Game { get; set; }
}


public abstract class Wizard : GameObject
{
    public abstract void DoSomething(List<GameObject> snaffles, List<GameObject> bludgers, int teamId);
    public bool Holding { get; set; }

    public static void Move(int x, int y, int thrust)
    {
        Console.WriteLine($"MOVE {x} {y} {thrust}");
    }

    public static void Throw(int x, int y, int power)
    {
        Console.WriteLine($"THROW {x} {y} {power}");
    }

    public static void Accio(int id)
    {
        Console.WriteLine($"ACCIO {id}");
    }

    public static void Flipendo(int id)
    {
        Console.WriteLine($"FLIPENDO {id}");
    }

    public static void Obliviate(int id)
    {
        Console.WriteLine($"OBLIVIATE {id}");
    }

    public static void Petrificus(int id)
    {
        Console.WriteLine($"PETRIFICUS {id}");
    }
}

public class Goalee : Wizard
{
    public override void DoSomething(List<GameObject> snaffles, List<GameObject> bludgers, int teamId)
    {
        throw new NotImplementedException();
    }
}


public class Forwarder : Wizard
{
    public static int ManaStashSize = Rules.AccioCost + Rules.FlipendoCost;
    public const int GoalDangerRadius = 2750;
    public const int AccioMin = 1000;
    public const int AccioMax = 5000;


    public GameObject Target { get; set; }
    public bool IsMasterPlayer { get; set; }
    public Forwarder Companion { get; set; }


    public override void DoSomething(List<GameObject> snaffles, List<GameObject> bludgers, int teamId)
    {
        var myGoalX = teamId == 0 ? 550 : Rules.FieldWidth - 550;
        var myGoalY = Rules.GoalCenter;

        if (Holding)
        {
            var destX = teamId == 0 ? Rules.FieldWidth : 0;
            var destY = Y < Rules.GoalTop ? Rules.GoalTop + 450 : Y > Rules.GoalBottom ? Rules.GoalBottom - 450 : Y;
            Throw(destX, destY, 500);
            Target = null;
            return;
        }

        if (Game.MyMagic > Rules.FlipendoCost)
        {
            var enemyGoalX = teamId == 0 ? Rules.FieldWidth : 0;
            var possibleFlipendo = snaffles
                .Where(s => Utils.IsInsideTriangle(s.X, s.Y, X, Y, enemyGoalX, Rules.GoalBottom - 500, enemyGoalX,
                    Rules.GoalTop + 500));
            if (possibleFlipendo.Any())
            {
                var flipTarget = teamId == 0
                    ? possibleFlipendo.OrderBy(x => -x.X).FirstOrDefault()
                    : possibleFlipendo.OrderBy(x => x.X).FirstOrDefault();
                Flipendo(flipTarget.Id);
                return;
            }
        }

        if (snaffles.Count > 2)
        {
            if (!IsMasterPlayer)
            {
                Target = snaffles.Where(s => s != Companion.Target)
                    .OrderBy(s => Utils.GetDistance(X, Y, s.X, s.Y))
                    .FirstOrDefault();
            }
            else
            {
                var orderedForFirst = snaffles
                    .OrderBy(s => Utils.GetDistance(X, Y, s.X, s.Y))
                    .ToList();
                var orderedForSecond = snaffles
                    .OrderBy(s => Utils.GetDistance(s.X, s.Y, Companion.X, Companion.Y))
                    .ToList();

                if (orderedForFirst[0] == orderedForSecond[0])
                {
                    var dist = Utils.GetDistance(X, Y, orderedForFirst[0].X, orderedForFirst[0].Y) +
                               Utils.GetDistance(Companion.X, Companion.Y, orderedForSecond[1].X,
                                   orderedForSecond[1].Y);

                    var swapDist = Utils.GetDistance(X, Y, orderedForFirst[1].X, orderedForFirst[1].Y) +
                                   Utils.GetDistance(Companion.X, Companion.Y, orderedForSecond[0].X,
                                       orderedForSecond[0].Y);

                    if (swapDist < dist)
                    {
                        Target = orderedForFirst[1];
                    }
                    else
                    {
                        Target = orderedForFirst[0];
                    }
                }
                else
                {
                    Target = orderedForFirst[0];
                }
            }
        }
        else
        {
            Target = snaffles.Count == 1 ? snaffles[0] : null;
        }

        if (Target == null)
        {
            Move(Rules.FieldWidth / 2, Rules.FieldHeight / 2, 150);
            return;
        }

        var dx = Target.X + Target.VX;
        var dy = Target.Y + Target.VY;
        Move(dx, dy, 150);
    }
}