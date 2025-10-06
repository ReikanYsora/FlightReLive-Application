/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#ifndef MK_EDGE_DETECTION_POST_PROCESSING_CONFIG
	#define MK_EDGE_DETECTION_POST_PROCESSING_CONFIG

    #ifndef MK_SELECTIVE_WORKFLOW_FAST_MODE
        //Uncomment this to make the selective workflow peform faster, but less precise
        //#define MK_SELECTIVE_WORKFLOW_FAST_MODE
    #endif

    #ifndef MK_EVALUATE_COMBINED_INPUT_EPSILON
        #define MK_EVALUATE_COMBINED_INPUT_EPSILON 0.0125
    #endif

    #ifndef MK_SELECTIVE_WORKFLOW_EPSILON
        #define MK_SELECTIVE_WORKFLOW_EPSILON 0.00000305
    #endif

    #ifndef MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON
        #define MK_SELECTIVE_WORKFLOW_SILHOUETTE_EPSILON 0.0125
    #endif

    #ifndef MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON
        #define MK_SELECTIVE_WORKFLOW_SILHOUETTE_FAST_EPSILON 1.0
    #endif

    #if !defined(MK_OPTIMIZE_UV) && defined(MK_RENDER_PIPELINE_HIGH_DEFINITION)
        #define MK_OPTIMIZE_UV
    #endif

    #if !defined(MK_EDGE_DETECTION_OFF) && !defined(MK_INPUT_DEPTH) && !defined(MK_INPUT_NORMAL) && !defined(MK_INPUT_SCENE_COLOR)
        #define MK_EDGE_DETECTION_OFF
    #endif

    #if !defined(MK_FOG) && (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
        #define MK_FOG
    #endif

    #if !defined(MK_PRECISION_HIGH) && !defined(MK_PRECISION_MEDIUM) && !defined(MK_PRECISION_LOW)
        #define MK_PRECISION_HIGH
    #endif

    #if !defined(MK_UTILIZE_BOX) && !defined(MK_UTILIZE_AXIS_ONLY) && !defined(MK_UTILIZE_DIAGONAL)
        #define MK_UTILIZE_BOX
    #endif

    #if !defined(MK_EVALUATE_COMBINED_INPUT) && defined(MK_ENHANCE_DETAILS) && (defined(MK_INPUT_NORMAL) && defined(MK_INPUT_DEPTH))
        #define MK_EVALUATE_COMBINED_INPUT
    #endif
#endif