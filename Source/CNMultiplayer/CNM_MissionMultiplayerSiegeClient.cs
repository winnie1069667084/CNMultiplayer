using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using System.Threading.Tasks;

namespace CNMultiplayer
{
    public class CNM_MissionMultiplayerSiegeClient : MissionMultiplayerSiegeClient, ICommanderInfo, IMissionBehavior
    {
        //暂时禁用临时音乐系统
        /*
        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _soundState = new int[9,2];
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            base.CheckTimer(out m, out n);
            if (m == 0 || IsInWarmup)
                return;
            if (_soundState[1, 0] != 2)
                _soundState[1, 0] = PlaySoundAtCamera("siege/start", 1, 3595, 60);
            if (_soundState[2, 0] != 2)
                _soundState[2, 0] = PlaySoundAtCamera("siege/music1", 2, 3500, 305);
            if (_soundState[3, 0] != 2)
                _soundState[3, 0] = PlaySoundAtCamera("siege/music2", 3, 3000, 163);
            if (_soundState[4, 0] != 2)
                _soundState[4, 0] = PlaySoundAtCamera("siege/music3", 4, 2750, 219);
            if (_soundState[5, 0] != 2)
                _soundState[5, 0] = PlaySoundAtCamera("siege/music4", 5, 2300, 254);
            if (_soundState[6, 0] != 2)
                _soundState[6, 0] = PlaySoundAtCamera("siege/music5", 6, 2000, 235);
            if (_soundState[7, 0] != 2)
                _soundState[7, 0] = PlaySoundAtCamera("siege/music6", 7, 1500, 224);
            if (_soundState[8, 0] != 2)
                _soundState[8, 0] = PlaySoundAtCamera("siege/music7", 8, 1200, 184);
        }

        // Delayed stop to prevent ambient sounds from looping (单位：s)
        private async void DelayedStop(SoundEvent eventRef, int soundDuration)
        {
            await Task.Delay(soundDuration * 1000);
            eventRef.Stop();
        }

        private int PlaySoundAtCamera(string eventString, int musicIndex, int playTime, int duration)
        {
            if (m == playTime && _soundState[musicIndex, 0] == 0)
            {
                MatrixFrame cameraFrame = Mission.Current.GetCameraFrame();
                Vec3 vec = cameraFrame.origin + cameraFrame.rotation.u;
                SoundEvent eventRef = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString(eventString), Mission.Current.Scene);
                eventRef.SetPosition(vec);
                eventRef.SetParameter("mpMusicSwitcher", 1f);
                eventRef.Play();
                DelayedStop(eventRef, duration);
                _soundState[musicIndex, 1] = eventRef.GetSoundId();
                return 1;
            }
            else if (_soundState[musicIndex, 0] != 2 && m <= playTime && m > playTime - duration)
                return 1;
            else if (m <= playTime - duration)
                return 2;
            return 0;
        }
        private int[,] _soundState;
        private int m, n;
        */
    }
}
