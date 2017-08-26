/*
    CelNoir config file

    Part of CelNoir cel shader package.
    
    This contains a few global definitions that allow you to
    tweak the behavior of the cel shaders.
*/

// The luminance threshold that the CelCrush function uses.
// Higher values will make the crush-to-black effect more
// aggressive, and create deeper regions of darkness on the
// screen.
// A threshold of 1 or higher will crush all colors to black.
// You probably don't want that. (Or anywhere near 1, for that
// matter.)
// A threshold of 0 or lower will cause the CelCrush function
// to always return the input color.
#define CELCRUSH_THRESHOLD 0.21875

// The number of distinct gradiations that should be permitted by the
// function that steps between different colors. More CELSTEPS
// means a more detailed lighting effect.
// Valid values range between 2 and 8.
// If CELSTEPS is out of range, the CelSteps function will
// simply return the input color.
#define CELSTEPS 5