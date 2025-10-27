using System;

namespace RoyTheunissen.AudioSyntax
{
    public static class IAudioPlaybackExtensions
    {
        public static IAudioPlayback WithParameter(this IAudioPlayback playback,
            string name, float value, bool ignoreSeekSpeed = false)
        {
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, value, ignoreSeekSpeed);
#endif
            return playback;
        }

        public static IAudioPlayback WithParameter(this IAudioPlayback playback,
            string name, int value, bool ignoreSeekSpeed = false)
        {
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, value, ignoreSeekSpeed);
#endif
            return playback;
        }

        public static IAudioPlayback WithParameter(this IAudioPlayback playback,
            string name, bool value, bool ignoreSeekSpeed = false)
        {
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, value ? 1f : 0f, ignoreSeekSpeed);
#endif
            return playback;
        }

        public static IAudioPlayback WithParameterLabel(this IAudioPlayback playback,
            string name, string label, bool ignoreSeekSpeed = false)
        {
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameterLabel(name, label, ignoreSeekSpeed);
#endif
            return playback;
        }

        public static IAudioPlayback WithParameter<TEnum>(this IAudioPlayback playback,
            string name, TEnum value, bool ignoreSeekSpeed = false)
            where TEnum : struct, Enum
        {
#if FMOD_AUDIO_SYNTAX
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, Convert.ToInt32(value), ignoreSeekSpeed);
#endif
            return playback;
        }

        public static IAudioPlayback WithParameters(this IAudioPlayback playback,
            params (string name, float value)[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
                playback.WithParameter(parameters[i].name, parameters[i].value);
            return playback;
        }

        public static IAudioPlayback WithVolume(this IAudioPlayback playback, float volume)
        {
            playback.Volume = volume;
            return playback;
        }
    }
}
