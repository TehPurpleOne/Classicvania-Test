using Godot;
using System;
using Array = Godot.Collections.Array;

public class AudioManager : Node
{
    public void playSound(string name) {
        Array sfx = new Array{};
        sfx = GetChild(0).GetChildren();

        for(int s = 0; s < sfx.Count; s++) {
            AudioStreamPlayer get = (AudioStreamPlayer)sfx[s];
            if(get.Name == name) {
                get.Play();
            }
        }
    }

    public void playMusic(string name) {
        Array mus = new Array{};
        mus = GetChild(1).GetChildren();

        for(int m = 0; m < mus.Count; m++) {
            AudioStreamPlayer get = (AudioStreamPlayer)mus[m];
            if(get.Name == name) {
                get.Play();
            }
        }
    }
}
