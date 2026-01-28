FeatureScript 2856;

// Pasted to Onshape from git commit: (fill in after paste)

import(path : "onshape/std/feature.fs", version : "2856.0");
import(path : "onshape/std/geometry.fs", version : "2856.0");

annotation { "Feature Type Name" : "My First Feature" }
export const myFirstFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Size", "Default" : 20 * millimeter }
        isLength(definition.size, LENGTH_BOUNDS);

        annotation { "Name" : "Origin X", "Default" : 0 * millimeter }
        isLength(definition.originX, LENGTH_BOUNDS);

        annotation { "Name" : "Origin Y", "Default" : 0 * millimeter }
        isLength(definition.originY, LENGTH_BOUNDS);

        annotation { "Name" : "Origin Z", "Default" : 0 * millimeter }
        isLength(definition.originZ, LENGTH_BOUNDS);

        annotation { "Name" : "Centered", "Default" : false }
        definition.centered is boolean;
    }
    {
        const boxId = id + "box";

        const origin = vector(definition.originX, definition.originY, definition.originZ);
        const sizeVec = vector(definition.size, definition.size, definition.size);
        const half = sizeVec / 2;

        const corner1 = definition.centered ? (origin - half) : origin;
        const corner2 = definition.centered ? (origin + half) : (origin + sizeVec);

        fCuboid(context, boxId, {
            "corner1" : corner1,
            "corner2" : corner2
        });
    });