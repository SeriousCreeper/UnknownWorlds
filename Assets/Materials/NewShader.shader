Shader "FX/Mirror Reflection" {

Properties {
	_Color ("Alpha=ratio", Color) = (0,0,0,0)
    _MainTex ("Base (RGB) Gloss (A)", 2D) = "white"
    _ReflectionTex ("Reflection (RGB) Gloss (A)", 2D) = "black" { TexGen ObjectLinear }
	//_ReflectColor ("Reflection Color", Color) = (1.0, 1.0, 1.0, 0)
}
Category {
Tags {Queue = Transparent}

// two texture cards: full thing
Subshader {
UsePass "Alpha/Glossy/BASE"
    Pass {
	Blend SrcAlpha OneMinusSrcAlpha
        SetTexture[_MainTex] { constantColor [_Color] combine texture }
        SetTexture[_ReflectionTex] { matrix [_ProjMatrix] constantColor [_Color]
            Combine texture * previous DOUBLE, texture * constant  }

    }
}

// fallback: just main texture
Subshader {
    Pass {
        SetTexture [_MainTex] { combine texture }
    }
}
}
}