using RageCoop.Client.Scripting;
using GTA.Native;
using GTA.UI;
using System.Drawing;
using GTA;
using System;
using System.Threading.Tasks;
using System.Threading;
using GTA.Math;
using System.Collections.Generic;

namespace RageCoop.Resources.Race
{
    public class Main : ClientScript
    {
        int _countdown=-1;
        Sprite _fadeoutSprite;
        readonly List<Vector3> _checkpoints = new List<Vector3>();
        Blip _nextBlip = null;
        Blip _secondBlip = null;

        public override void OnStart()
        {
            API.RegisterCustomEventHandler(Events.CountDown, CountDown);
            API.RegisterCustomEventHandler(Events.StartCheckpointSequence, Checkpoints);
            API.RegisterCustomEventHandler(Events.LeaveRace, LeaveRace);
            API.Events.OnTick+=OnTick;
            API.QueueAction(() => { Function.Call(Hash.ON_ENTER_MP); });
        }

        private void OnTick()
        {
            if (_countdown > -1 && _countdown <= 3)
            {
                var res = ResolutionMaintainRatio;
                new LemonUI.Elements.ScaledText(new Point((int)res.Width/2,260*1080/720), _countdown == 0 ? "GO" : _countdown.ToString()) 
                {
                    Alignment = Alignment.Center,
                    Scale=2f,
                    Font=GTA.UI.Font.Pricedown,
                    Color=Color.White 
                }.Draw();
            }
            if (_fadeoutSprite?.Color.A > 2)
            {
                _fadeoutSprite.Color = Color.FromArgb(_fadeoutSprite.Color.A - 2, _fadeoutSprite.Color.R, _fadeoutSprite.Color.G,
                    _fadeoutSprite.Color.B);
                _fadeoutSprite.Draw();
            }

            if (_checkpoints.Count > 0)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, _checkpoints[0], new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(10f, 10f, 2f), Color.FromArgb(100, 241, 247, 57));
                if (_nextBlip == null)
                    _nextBlip = World.CreateBlip(_checkpoints[0]);

                if (_checkpoints.Count > 1)
                {
                    if (_secondBlip == null)
                    {
                        _secondBlip = World.CreateBlip(_checkpoints[1]);
                        _secondBlip.Scale = 0.5f;
                        if (_checkpoints.Count == 2)
                            _secondBlip.Sprite = BlipSprite.RaceFinish;
                    }
                    Vector3 dir = _checkpoints[1] - _checkpoints[0];
                    dir.Normalize();
                    World.DrawMarker(MarkerType.ChevronUpx1, _checkpoints[0] + new Vector3(0f, 0f, 2f), dir, new Vector3(60f, 0f, 0f), new Vector3(4f, 4f, 4f), Color.FromArgb(200, 87, 193, 250));
                }
                else
                {
                    Vector3 dir = Game.Player.Character.Position - _checkpoints[0];
                    dir.Normalize();
                    World.DrawMarker(MarkerType.CheckeredFlagRect, _checkpoints[0] + new Vector3(0f, 0f, 2f), dir, new Vector3(0f, 0f, 0f), new Vector3(4f, 4f, 4f), Color.FromArgb(200, 87, 193, 250));
                    _nextBlip.Sprite = BlipSprite.RaceFinish;
                }

                if (Game.Player.Character.IsInVehicle() && Game.Player.Character.IsInRange(_checkpoints[0], 10f))
                {
                    Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
                    _checkpoints.RemoveAt(0);
                    ClearBlips();
                }
            }
        }
        private void CountDown(CustomEventReceivedArgs obj)
        {
            Task.Run(() =>
            {
                for (_countdown=3;_countdown>=0; _countdown--)
                {
                    API.QueueAction(() =>
                    {
                        var w = Convert.ToInt32(Screen.Width / 2);
                        _fadeoutSprite = new Sprite("mpinventory", "in_world_circle", new SizeF(200, 200), new PointF(w - 100, 200), _countdown == 0 ? Color.FromArgb(150,49, 235, 126) : Color.FromArgb(150,241, 247, 57));
                        Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
                    });
                    Thread.Sleep(1000);
                }
            });
        }

        private void Checkpoints(CustomEventReceivedArgs obj)
        {
            _checkpoints.Clear();
            foreach (var item in obj.Args)
                _checkpoints.Add((Vector3)item);
            API.QueueAction(() => { ClearBlips(); });
        }

        private void LeaveRace(CustomEventReceivedArgs obj)
        {
            _checkpoints.Clear();
            API.QueueAction(() => { ClearBlips(); });
        }

        private void ClearBlips()
        {
            _nextBlip?.Delete();
            _secondBlip?.Delete();
            _nextBlip = null;
            _secondBlip = null;
        }

        public override void OnStop()
        {
            API.QueueAction(() =>
            {
                ClearBlips();
                Function.Call(Hash.ON_ENTER_SP);
            });
        }
        public static SizeF ResolutionMaintainRatio
        {
            get
            {
                // Get the game width and height
                int screenw = Screen.Resolution.Width;
                int screenh = Screen.Resolution.Height;
                // Calculate the ratio
                float ratio = (float)screenw / screenh;
                // And the width with that ratio
                float width = 1080f * ratio;
                // Finally, return a SizeF
                return new SizeF(width, 1080f);
            }
        }
    }
}