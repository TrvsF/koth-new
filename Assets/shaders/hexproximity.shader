//=========================================================================================================
// Hex Proximity
//
// World-locked hexagon grid that fades in near a focus point (usually the player).
// All parameters are render attributes, driven by WallHexProjector.cs.
//=========================================================================================================

HEADER
{
	Description = "Hexagon proximity grid";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
}

COMMON
{
	#include "common/shared.hlsl"
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
	#include "common/pixel.hlsl"

	RenderState( BlendEnable, true );
	RenderState( SrcBlend, SRC_ALPHA );
	RenderState( DstBlend, ONE );
	RenderState( BlendOp, ADD );
	RenderState( DepthWriteEnable, false );
	RenderState( CullMode, NONE );

	float3 g_vFocusPos < Attribute( "FocusPos" ); >;
	float g_flHexSize < Attribute( "HexSize" ); Default( 8.0 ); >;
	float g_flThickness < Attribute( "Thickness" ); Default( 0.07 ); >;
	float g_flFill < Attribute( "Fill" ); Default( 0.06 ); >;
	float g_flRange < Attribute( "Range" ); Default( 80.0 ); >;
	float g_flFalloff < Attribute( "Falloff" ); Default( 2.0 ); >;
	float g_flPulse < Attribute( "Pulse" ); Default( 0.35 ); >;
	float3 g_vTint < Attribute( "Tint" ); Default3( 0.2, 0.9, 1.0 ); >;
	float g_flBrightness < Attribute( "Brightness" ); Default( 2.0 ); >;

	//
	// Returns xy = offset from the nearest hex centre (in cell units)
	//         zw = id of that hex cell
	//
	float4 HexCell( float2 p )
	{
		float2 vSpacing = float2( 1.0, 1.7320508 );

		float4 hC = floor( float4( p, p - float2( 0.5, 1.0 ) ) / vSpacing.xyxy ) + 0.5;
		float4 h = float4( p - hC.xy * vSpacing, p - ( hC.zw + 0.5 ) * vSpacing );

		if ( dot( h.xy, h.xy ) < dot( h.zw, h.zw ) )
		{
			return float4( h.xy, hC.xy );
		}

		return float4( h.zw, hC.zw + 0.5 );
	}

	//
	// Hexagonal distance field. 0 at the centre of the cell, 0.5 on the edge.
	//
	float HexEdgeDist( float2 p )
	{
		float2 q = abs( p );
		return max( dot( q, float2( 0.5, 0.8660254 ) ), q.x );
	}

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		// Positions are camera-relative for precision, so add the camera back on.
		float3 vPositionWs = i.vPositionWithOffsetWs.xyz + g_vCameraPositionWs.xyz;
		float3 vNormalWs = normalize( i.vNormalWs.xyz );

		// Stable tangent basis from the normal, so the grid is locked to world space
		// rather than to the quad UVs. Panels meeting at a corner line up.
		float3 vRef = float3( 0.0, 0.0, 1.0 );

		if ( abs( vNormalWs.z ) > 0.99 )
		{
			vRef = float3( 1.0, 0.0, 0.0 );
		}

		float3 vT = normalize( cross( vRef, vNormalWs ) );
		float3 vB = cross( vNormalWs, vT );

		float flSize = max( g_flHexSize, 0.01 );
		float2 vUv = float2( dot( vPositionWs, vT ), dot( vPositionWs, vB ) ) / flSize;

		float4 hex = HexCell( vUv );
		float flDist = HexEdgeDist( hex.xy );

		// Antialiased outline: 1 on the border, 0 inside the cell.
		float flAa = fwidth( flDist ) * 1.5 + 0.00001;
		float flEdge = 0.5 - g_flThickness;
		float flLine = smoothstep( flEdge - flAa, flEdge + flAa, flDist );

		// Per-cell distance to the focus point, so whole hexagons light up at once.
		float2 vCellUv = ( vUv - hex.xy ) * flSize;
		float3 vCellWs = vT * vCellUv.x + vB * vCellUv.y + vNormalWs * dot( vPositionWs, vNormalWs );

		float flFocusDist = distance( vCellWs, g_vFocusPos );
		float flFade = saturate( 1.0 - flFocusDist / max( g_flRange, 0.01 ) );
		flFade = pow( flFade, g_flFalloff );

		// Ring travelling outwards from the focus point.
		float flWave = sin( g_flTime * 3.0 - flFocusDist * 0.12 ) * 0.5 + 0.5;
		flFade = flFade * lerp( 1.0, flWave, g_flPulse );

		float flAlpha = ( flLine + ( 1.0 - flLine ) * g_flFill ) * flFade;
		float3 vColor = g_vTint * g_flBrightness;

		return float4( vColor, saturate( flAlpha ) );
	}
}
