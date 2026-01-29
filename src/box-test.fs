FeatureScript 2856;

// git commit 'Simplify manipulator normalization'

import(path : "onshape/std/feature.fs", version : "2856.0");
import(path : "onshape/std/geometry.fs", version : "2856.0");
export import(path : "onshape/std/tool.fs", version : "2856.0");

export enum PlacementMode
{
    annotation { "Name" : "Center" }
    CENTER,
    annotation { "Name" : "Face Center" }
    FACE_CENTER,
    annotation { "Name" : "Corner" }
    CORNER
}


export enum DraftNeutralPlane
{
    annotation { "Name" : "Top" }
    TOP,
    annotation { "Name" : "Middle" }
    MIDDLE,
    annotation { "Name" : "Bottom" }
    BOTTOM
}

const MIN_SIZE = 0.01 * millimeter;

annotation { "Feature Type Name" : "Box Test", "Manipulator Change Function" : "boxTestManipulators" }
export const boxTest = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        booleanStepTypePredicate(definition);

        annotation { "Name" : "Placement", "UIHint" : UIHint.HORIZONTAL_ENUM, "Default" : PlacementMode.CENTER }
        definition.placement is PlacementMode;

        annotation {
            "Name" : "Location",
            "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR,
            "MaxNumberOfPicks" : 1
        }
        definition.location is Query;

        annotation { "Name" : "X Size" }
        isLength(definition.sizeX, NONNEGATIVE_LENGTH_BOUNDS);
        if (definition.placement == PlacementMode.CORNER)
        {
            annotation { "Name" : "", "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : false }
            definition.flipX is boolean;
        }

        annotation { "Name" : "Y Size" }
        isLength(definition.sizeY, NONNEGATIVE_LENGTH_BOUNDS);
        if (definition.placement == PlacementMode.CORNER)
        {
            annotation { "Name" : "", "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : false }
            definition.flipY is boolean;
        }

        annotation { "Name" : "Z Size" }
        isLength(definition.sizeZ, NONNEGATIVE_LENGTH_BOUNDS);
        if (definition.placement == PlacementMode.CORNER || definition.placement == PlacementMode.FACE_CENTER)
        {
            annotation { "Name" : "", "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : false }
            definition.flipZ is boolean;
        }


        annotation { "Name" : "X Offset" }
        isLength(definition.originX, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Y Offset" }
        isLength(definition.originY, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Z Offset" }
        isLength(definition.originZ, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Draft", "Default" : false }
        definition.hasDraft is boolean;
        if (definition.hasDraft)
        {
            annotation { "Name" : "Neutral plane", "Default" : DraftNeutralPlane.MIDDLE }
            definition.draftNeutralPlane is DraftNeutralPlane;

            annotation { "Name" : "Draft angle", "Default" : 1 * degree }
            isAngle(definition.draftAngle, ANGLE_STRICT_90_BOUNDS);

            annotation { "Name" : "", "UIHint" : UIHint.OPPOSITE_DIRECTION, "Default" : false }
            definition.reverseDraft is boolean;
        }

        if (definition.operationType != NewBodyOperationType.NEW)
        {
            annotation { "Name" : "Merge with all", "Default" : true }
            definition.defaultScope is boolean;
            if (definition.defaultScope != true)
            {
                annotation { "Name" : "Merge scope", "Filter" : EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES }
                definition.booleanScope is Query;
            }
        }

    }
    {
        const boxId = id + "box";

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
                baseCsys = coordSystem(point, X_DIRECTION, Z_DIRECTION);
            }
        }

        const remainingTransform = getRemainderPatternTransform(context, {
            "references" : definition.location
        });

        const transformedOrigin = remainingTransform * baseCsys.origin;
        const transformedXAxis = normalize((remainingTransform * (baseCsys.origin + baseCsys.xAxis * meter)) - transformedOrigin);
        const transformedZAxis = normalize((remainingTransform * (baseCsys.origin + baseCsys.zAxis * meter)) - transformedOrigin);
        baseCsys = coordSystem(transformedOrigin, transformedXAxis, transformedZAxis);

        const origin = vector(definition.originX, definition.originY, definition.originZ);
        const clampedSizeX = max(definition.sizeX, MIN_SIZE);
        const clampedSizeY = max(definition.sizeY, MIN_SIZE);
        const clampedSizeZ = max(definition.sizeZ, MIN_SIZE);
        const sizeVec = vector(clampedSizeX, clampedSizeY, clampedSizeZ);

        var localCorner1;
        var localCorner2;
        var signedSize;
        if (definition.placement == PlacementMode.CENTER)
        {
            const half = sizeVec / 2;
            signedSize = sizeVec;
            localCorner1 = origin - half;
            localCorner2 = origin + half;
        }
        else if (definition.placement == PlacementMode.FACE_CENTER)
        {
            const halfXY = vector(clampedSizeX / 2, clampedSizeY / 2, 0 * millimeter);
            const signZ = definition.flipZ ? -1 : 1;
            const signedSizeZ = signZ * clampedSizeZ;
            signedSize = vector(clampedSizeX, clampedSizeY, signedSizeZ);
            const zMin = min(0 * millimeter, signedSizeZ);
            const zMax = max(0 * millimeter, signedSizeZ);
            localCorner1 = origin - halfXY + vector(0 * millimeter, 0 * millimeter, zMin);
            localCorner2 = origin + halfXY + vector(0 * millimeter, 0 * millimeter, zMax);
        }
        else
        {
            const signX = definition.flipX ? -1 : 1;
            const signY = definition.flipY ? -1 : 1;
            const signZ = definition.flipZ ? -1 : 1;
            signedSize = vector(signX * clampedSizeX, signY * clampedSizeY, signZ * clampedSizeZ);
            localCorner1 = origin;
            localCorner2 = origin + signedSize;
        }




        const localCenter = (localCorner1 + localCorner2) / 2;

        var reconstructOp = function()
        {
            fCuboid(context, boxId, {
                "corner1" : localCorner1,
                "corner2" : localCorner2
            });

            if (definition.hasDraft == true)
            {
                const faceEntities = evaluateQuery(context, qCreatedBy(boxId, EntityType.FACE));
                var sideFaceQueries = [];
                var topFace;
                var bottomFace;
                var topZ = -1e9 * meter;
                var bottomZ = 1e9 * meter;
                for (var face in faceEntities)
                {
                    const planeDef = evFaceTangentPlane(context, {
                        "face" : face,
                        "parameter" : vector(0.5, 0.5)
                    });
                    const normal = planeDef.normal;
                    const dotZ = dot(normal, Z_DIRECTION);
                    if (abs(dotZ) > 0.999)
                    {
                        const zPos = dot(planeDef.origin, Z_DIRECTION);
                        if (zPos > topZ)
                        {
                            topZ = zPos;
                            topFace = face;
                        }
                        if (zPos < bottomZ)
                        {
                            bottomZ = zPos;
                            bottomFace = face;
                        }
                    }
                    else
                    {
                        sideFaceQueries = append(sideFaceQueries, face);
                    }
                }

                if (size(sideFaceQueries) > 0 && topFace != undefined && bottomFace != undefined)
                {
                    const draftFaces = qUnion(sideFaceQueries);
                    var neutralPlaneQuery;
                    const pullVec = definition.reverseDraft == true ? -Z_DIRECTION : Z_DIRECTION;

                    if (definition.draftNeutralPlane == DraftNeutralPlane.TOP)
                    {
                        neutralPlaneQuery = topFace;
                    }
                    else if (definition.draftNeutralPlane == DraftNeutralPlane.BOTTOM)
                    {
                        neutralPlaneQuery = bottomFace;
                    }
                    else
                    {
                        const planeSize = max(clampedSizeX, clampedSizeY) * 2;
                        const planeId = id + "draftPlane";
                        opPlane(context, planeId, {
                            "plane" : plane(localCenter, Z_DIRECTION),
                            "width" : planeSize,
                            "height" : planeSize
                        });
                        neutralPlaneQuery = qCreatedBy(planeId, EntityType.FACE);
                    }

                    opDraft(context, id + "draft", {
                        "draftType" : DraftType.NEUTRAL_PLANE,
                        "neutralPlane" : neutralPlaneQuery,
                        "draftFaces" : draftFaces,
                        "pullVec" : pullVec,
                        "angle" : definition.draftAngle
                    });

                    if (definition.draftNeutralPlane == DraftNeutralPlane.MIDDLE)
                    {
                        opDeleteBodies(context, id + "draftPlaneDelete", {
                            "entities" : qCreatedBy(id + "draftPlane", EntityType.BODY)
                        });
                    }
                }
            }

            opTransform(context, id + "orientBox", {
                "bodies" : qCreatedBy(boxId, EntityType.BODY),
                "transform" : toWorld(baseCsys)
            });
        };
        reconstructOp();

        const worldCenter = toWorld(baseCsys, localCenter);
        const yAxis = normalize(cross(baseCsys.zAxis, baseCsys.xAxis));

        var xManip;
        var yManip;
        var zManip;
        var diagManip;
        if (definition.placement == PlacementMode.CENTER)
        {
            xManip = linearManipulator({
                "base" : worldCenter,
                "direction" : baseCsys.xAxis,
                "offset" : signedSize[0] / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeX"
            });
            yManip = linearManipulator({
                "base" : worldCenter,
                "direction" : yAxis,
                "offset" : signedSize[1] / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeY"
            });
            zManip = linearManipulator({
                "base" : worldCenter,
                "direction" : baseCsys.zAxis,
                "offset" : signedSize[2] / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeZ"
            });
            diagManip = linearManipulator({
                "base" : worldCenter,
                "direction" : normalize(
                    baseCsys.xAxis * signedSize[0] +
                    yAxis * signedSize[1] +
                    baseCsys.zAxis * signedSize[2]
                ),
                "offset" : norm(signedSize) / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeX"
            });
        }
        else if (definition.placement == PlacementMode.FACE_CENTER)
        {
            const worldOrigin = toWorld(baseCsys, origin);
            xManip = linearManipulator({
                "base" : worldCenter,
                "direction" : baseCsys.xAxis,
                "offset" : signedSize[0] / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeX"
            });
            yManip = linearManipulator({
                "base" : worldCenter,
                "direction" : yAxis,
                "offset" : signedSize[1] / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeY"
            });
            zManip = linearManipulator({
                "base" : worldOrigin,
                "direction" : baseCsys.zAxis,
                "offset" : signedSize[2],
                "primaryParameterId" : "sizeZ"
            });
            diagManip = linearManipulator({
                "base" : worldCenter,
                "direction" : normalize(
                    baseCsys.xAxis * signedSize[0] +
                    yAxis * signedSize[1] +
                    baseCsys.zAxis * signedSize[2]
                ),
                "offset" : norm(signedSize) / 2,
                "minValue" : MIN_SIZE / 2,
                "primaryParameterId" : "sizeX"
            });
        }
        else
        {
            const xBase = toWorld(baseCsys, origin + vector(0 * millimeter, signedSize[1] / 2, signedSize[2] / 2));
            const yBase = toWorld(baseCsys, origin + vector(signedSize[0] / 2, 0 * millimeter, signedSize[2] / 2));
            const zBase = toWorld(baseCsys, origin + vector(signedSize[0] / 2, signedSize[1] / 2, 0 * millimeter));
            const diagBase = toWorld(baseCsys, origin);
            const diagDir = normalize(
                baseCsys.xAxis * signedSize[0] +
                yAxis * signedSize[1] +
                baseCsys.zAxis * signedSize[2]
            );

            xManip = linearManipulator({
                "base" : xBase,
                "direction" : baseCsys.xAxis,
                "offset" : signedSize[0],
                "primaryParameterId" : "sizeX"
            });
            yManip = linearManipulator({
                "base" : yBase,
                "direction" : yAxis,
                "offset" : signedSize[1],
                "primaryParameterId" : "sizeY"
            });
            zManip = linearManipulator({
                "base" : zBase,
                "direction" : baseCsys.zAxis,
                "offset" : signedSize[2],
                "primaryParameterId" : "sizeZ"
            });
            diagManip = linearManipulator({
                "base" : diagBase,
                "direction" : diagDir,
                "offset" : norm(signedSize),
                "primaryParameterId" : "sizeX"
            });
        }

        addManipulators(context, id, {
            "xSize" : xManip,
            "ySize" : yManip,
            "zSize" : zManip,
            "diagSize" : diagManip
        });


        const toolBodies = qCreatedBy(boxId, EntityType.BODY);
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
    if (definition.flipX == undefined)
    {
        definition.flipX = false;
    }
    if (definition.flipY == undefined)
    {
        definition.flipY = false;
    }
    if (definition.flipZ == undefined)
    {
        definition.flipZ = false;
    }

    definition.sizeX = max(definition.sizeX, MIN_SIZE);
    definition.sizeY = max(definition.sizeY, MIN_SIZE);
    definition.sizeZ = max(definition.sizeZ, MIN_SIZE);
    return definition;
}

export function boxTestManipulators(context is Context, definition is map, newManipulators is map) returns map
{
    definition = normalizeManipulatorDefinition(definition);

    if (newManipulators["xSize"] != undefined)
    {
        const manip = newManipulators["xSize"];
        if (manip.offset == undefined)
        {
            return definition;
        }
        if (definition.placement != PlacementMode.CORNER && abs(manip.offset) < MIN_SIZE / 2)
        {
            return definition;
        }
        if (definition.placement != PlacementMode.CORNER)
        {
            definition.sizeX = max(abs(manip.offset) * 2, MIN_SIZE);
        }
        else
        {
            definition.sizeX = max(abs(manip.offset), MIN_SIZE);
            definition.flipX = manip.offset < 0 * millimeter;
        }
        return normalizeManipulatorDefinition(definition);
    }
    if (newManipulators["ySize"] != undefined)
    {
        const manip = newManipulators["ySize"];
        if (manip.offset == undefined)
        {
            return definition;
        }
        if (definition.placement != PlacementMode.CORNER && abs(manip.offset) < MIN_SIZE / 2)
        {
            return definition;
        }
        if (definition.placement != PlacementMode.CORNER)
        {
            definition.sizeY = max(abs(manip.offset) * 2, MIN_SIZE);
        }
        else
        {
            definition.sizeY = max(abs(manip.offset), MIN_SIZE);
            definition.flipY = manip.offset < 0 * millimeter;
        }
        return normalizeManipulatorDefinition(definition);
    }
    if (newManipulators["zSize"] != undefined)
    {
        const manip = newManipulators["zSize"];
        if (manip.offset == undefined)
        {
            return definition;
        }
        if (definition.placement == PlacementMode.CENTER && abs(manip.offset) < MIN_SIZE / 2)
        {
            return definition;
        }
        if (definition.placement == PlacementMode.FACE_CENTER && abs(manip.offset) < MIN_SIZE)
        {
            return definition;
        }
        if (definition.placement == PlacementMode.CENTER)
        {
            definition.sizeZ = max(abs(manip.offset) * 2, MIN_SIZE);
        }
        else
        {
            definition.sizeZ = max(abs(manip.offset), MIN_SIZE);
            definition.flipZ = manip.offset < 0 * millimeter;
        }
        return normalizeManipulatorDefinition(definition);
    }
    if (newManipulators["diagSize"] != undefined)
    {
        const manip = newManipulators["diagSize"];
        if (manip.offset == undefined)
        {
            return definition;
        }
        if (definition.placement != PlacementMode.CORNER && abs(manip.offset) < MIN_SIZE / 2)
        {
            return definition;
        }
        const flipX = definition.flipX == true;
        const flipY = definition.flipY == true;
        const flipZ = definition.flipZ == true;
        const signX = flipX ? -1 : 1;
        const signY = flipY ? -1 : 1;
        const signZ = flipZ ? -1 : 1;
        const clampedSizeX = max(definition.sizeX, MIN_SIZE);
        const clampedSizeY = max(definition.sizeY, MIN_SIZE);
        const clampedSizeZ = max(definition.sizeZ, MIN_SIZE);
        const signedSize = vector(signX * clampedSizeX, signY * clampedSizeY, signZ * clampedSizeZ);
        const oldOffset = definition.placement != PlacementMode.CORNER ? norm(signedSize) / 2 : norm(signedSize);
        if (oldOffset <= 0 * millimeter)
        {
            return definition;
        }

        var newOffset = manip.offset;
        if (newOffset == undefined)
        {
            return definition;
        }
        if (definition.placement == PlacementMode.CORNER && newOffset < 0 * millimeter)
        {
            definition.flipX = !flipX;
            definition.flipY = !flipY;
            definition.flipZ = !flipZ;
            newOffset = -newOffset;
        }

        const scale = newOffset / oldOffset;
        definition.sizeX = max(abs(definition.sizeX * scale), MIN_SIZE);
        definition.sizeY = max(abs(definition.sizeY * scale), MIN_SIZE);
        definition.sizeZ = max(abs(definition.sizeZ * scale), MIN_SIZE);
        return normalizeManipulatorDefinition(definition);
    }
    return definition;
}
