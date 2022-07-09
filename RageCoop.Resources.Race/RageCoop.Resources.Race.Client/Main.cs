using RageCoop.Client.Scripting;
using GTA.Native;
using GTA.UI;
using System.Drawing;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace RageCoop.Resources.Race
{
    public class Main : ClientScript
    {
        int _countdown=-1;
        Sprite _fadeoutSprite;
        public override void OnStart()
        {
            API.RegisterCustomEventHandler(Events.CountDown, CountDown);
            API.Events.OnTick+=OnTick;
        }

        private void OnTick()
        {
            if (_countdown > -1 && _countdown <= 3)
            {
                var res = Screen.Resolution;
                new LemonUI.Elements.ScaledText(new Point(res.Width/2, res.Height/2-45), _countdown == 0 ? "GO" : _countdown.ToString()) 
                {
                    Alignment = Alignment.Center,
                    Scale=2f,
                    Font=GTA.UI.Font.Pricedown,
                    Color=Color.White 
                }.Draw();
            }
            if (_fadeoutSprite?.Color.A > 5)
            {
                _fadeoutSprite.Color = Color.FromArgb(_fadeoutSprite.Color.A - 5, _fadeoutSprite.Color.R, _fadeoutSprite.Color.G,
                    _fadeoutSprite.Color.B);
                _fadeoutSprite.Draw();
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
                        var h = Convert.ToInt32(Screen.Height / 2);
                        _fadeoutSprite = new Sprite("mpinventory", "in_world_circle", new SizeF(200, 200), new PointF(w - 100, h - 100), _countdown == 0 ? Color.FromArgb(49, 235, 126) : Color.FromArgb(241, 247, 57));
                        Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
                    });
                    Thread.Sleep(1000);
                }
            });
        }

        public override void OnStop()
        {
            
        }
    }
}