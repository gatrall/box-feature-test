FeatureScript 2856;

// git commit 'Template placeholder'
// Feature template: standard structure for Onshape custom features.
// Notes:
// - Keep axis conventions explicit (what is local X/Y/Z, what does "top/bottom" mean).
// - If location is a sketch vertex, orient from sketch plane normal.
// - For patterning, apply remainder transform to the reference CSys before building geometry.
// - For booleans, use tool.fs helpers (booleanStepTypePredicate / processNewBodyIfNeeded).

import(path : "onshape/std/feature.fs", version : "2856.0");
import(path : "onshape/std/geometry.fs", version : "2856.0");
import(path : "onshape/std/sketch.fs", version : "2856.0");
export import(path : "onshape/std/tool.fs", version : "2856.0");

// Example enum (replace or remove).
export enum PlacementMode
{
    annotation { "Name" : "Center" }
    CENTER,
    annotation { "Name" : "Corner" }
    CORNER
}

// Constants.
const MIN_SIZE = 0.01 * millimeter;
const DRAFT_ANGLE_MAX = 89.9 * degree; // Standard opDraft limit

annotation { "Feature Type Name" : "Template Feature", "Manipulator Change Function" : "templateManipulators" }
export const templateFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Standard boolean UI (New/Add/Remove/Intersect).
        booleanStepTypePredicate(definition);

        // Location picker (sketch point or mate connector).
        annotation {
            "Name" : "Location",
            "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR,
            "MaxNumberOfPicks" : 1
        }
        definition.location is Query;

        // Example params.
        annotation { "Name" : "Placement", "UIHint" : UIHint.HORIZONTAL_ENUM, "Default" : PlacementMode.CENTER }
        definition.placement is PlacementMode;

        annotation { "Name" : "X Size" }
        isLength(definition.sizeX, NONNEGATIVE_LENGTH_BOUNDS);

        annotation { "Name" : "Y Size" }
        isLength(definition.sizeY, NONNEGATIVE_LENGTH_BOUNDS);

        annotation { "Name" : "Z Size" }
        isLength(definition.sizeZ, NONNEGATIVE_LENGTH_BOUNDS);

        // Offsets (default to zero).
        annotation { "Name" : "X Offset" }
        isLength(definition.originX, ZERO_DEFAULT_LENGTH_BOUNDS);
        annotation { "Name" : "Y Offset" }
        isLength(definition.originY, ZERO_DEFAULT_LENGTH_BOUNDS);
        annotation { "Name" : "Z Offset" }
        isLength(definition.originZ, ZERO_DEFAULT_LENGTH_BOUNDS);

        // Boolean scope UI.
        booleanStepScopePredicate(definition);
    }
    {
        const bodyId = id + "body";

        // Base CSys from location.
        var baseCsys = WORLD_COORD_SYSTEM;
        if (size(evaluateQuery(context, definition.location)) > 0)
        {
            const mateCsys = try silent(evMateConnector(context, {
                "mateConnector" : definition.location
            }));
            if (mateCsys != undefined)
            {
                baseCsys = mateCsys;
            }
            else
            {
                const point = evVertexPoint(context, {
                    "vertex" : definition.location
                });
                const sketchPlane = try silent(evOwnerSketchPlane(context, {
                    "entity" : definition.location
                }));
                if (sketchPlane != undefined)
                {
                    baseCsys = coordSystem(point, sketchPlane.x, sketchPlane.normal);
                }
                else
                {
                    baseCsys = coordSystem(point, X_DIRECTION, Z_DIRECTION);
                }
            }
        }

        // Feature pattern remainder transform.
        const remainingTransform = getRemainderPatternTransform(context, {
            "references" : definition.location
        });
        const transformedOrigin = remainingTransform * baseCsys.origin;
        const transformedXAxis = normalize((remainingTransform * (baseCsys.origin + baseCsys.xAxis * meter)) - transformedOrigin);
        const transformedZAxis = normalize((remainingTransform * (baseCsys.origin + baseCsys.zAxis * meter)) - transformedOrigin);
        baseCsys = coordSystem(transformedOrigin, transformedXAxis, transformedZAxis);

        // Local geometry setup.
        const origin = vector(definition.originX, definition.originY, definition.originZ);
        const clampedSizeX = max(definition.sizeX, MIN_SIZE);
        const clampedSizeY = max(definition.sizeY, MIN_SIZE);
        const clampedSizeZ = max(definition.sizeZ, MIN_SIZE);
        const sizeVec = vector(clampedSizeX, clampedSizeY, clampedSizeZ);
        const half = sizeVec / 2;
        const localCorner1 = origin - half;
        const localCorner2 = origin + half;

        // Build geometry in local coords, then orient.
        var reconstructOp = function()
        {
            fCuboid(context, bodyId, {
                "corner1" : localCorner1,
                "corner2" : localCorner2
            });

            opTransform(context, id + "orientBody", {
                "bodies" : qCreatedBy(bodyId, EntityType.BODY),
                "transform" : toWorld(baseCsys)
            });
        };
        reconstructOp();

        // Manipulators (placeholder).
        // addManipulators(context, id, { ... });

        // Boolean handling (standard).
        const toolBodies = qCreatedBy(bodyId, EntityType.BODY);
        definition.mergeScopeExclusion = toolBodies;
        processNewBodyIfNeeded(context, id, definition, reconstructOp);
    });

function normalizeManipulatorDefinition(definition is map) returns map
{
    if (definition.placement == undefined)
    {
        definition.placement = PlacementMode.CENTER;
    }
    if (definition.sizeX == undefined)
    {
        definition.sizeX = MIN_SIZE;
    }
    if (definition.sizeY == undefined)
    {
        definition.sizeY = MIN_SIZE;
    }
    if (definition.sizeZ == undefined)
    {
        definition.sizeZ = MIN_SIZE;
    }

    definition.sizeX = max(definition.sizeX, MIN_SIZE);
    definition.sizeY = max(definition.sizeY, MIN_SIZE);
    definition.sizeZ = max(definition.sizeZ, MIN_SIZE);
    return definition;
}

export function templateManipulators(context is Context, definition is map, newManipulators is map) returns map
{
    definition = normalizeManipulatorDefinition(definition);

    // Example manip handler.
    // if (newManipulators["xSize"] != undefined) { ... }

    return definition;
}
