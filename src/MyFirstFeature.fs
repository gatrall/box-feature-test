FeatureScript 2856;

// Pasted to Onshape from git commit: (fill in later)

import(path : "onshape/std/feature.fs", version : "2856.0");
import(path : "onshape/std/geometry.fs", version : "2856.0");
import(path : "onshape/std/boolean.fs", version : "2856.0");
import(path : "onshape/std/units.fs", version : "2856.0");

annotation { "Feature Type Name" : "My First Feature" }
export const myFirstFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Size", "Default" : 25 * millimeter }
        isLength(definition.size, LENGTH_BOUNDS);

        annotation { "Name" : "Origin", "Default" : vector(0, 0, 0) * millimeter }
        isVector(definition.origin);
    }
    {
        const boxId = id + "box";

        opBox(context, boxId, {
            "corner1" : definition.origin,
            "corner2" : definition.origin + vector(definition.size, definition.size, definition.size)
        });
    });