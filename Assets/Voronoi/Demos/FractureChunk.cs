using UnityEngine;
using System.Collections.Generic;

using Voronoi;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class FractureChunk : MonoBehaviour
{
    public Material material;

    private Mesh meshSunnyside, meshDouble;

    public Cell cell;

    public bool separated = false;

    float forceAccumulation = 0.0f;

    public void ApplyForce(Vector3 impactPoint)
    {
        GetComponent<MeshFilter>().sharedMesh = meshDouble;

        forceAccumulation += 1;
        float d = 5.0f;
        transform.Rotate(new Vector3(Random.Range(-d, d), Random.Range(-d, d), Random.Range(-d, d)));

        if (forceAccumulation >= 1)
        {
            //separated = true;
            var pos = transform.position;
            pos.y = 1;
            var c = Instantiate(gameObject, pos, Quaternion.identity) as GameObject;

            Rigidbody rigidBody = c.GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            rigidBody.useGravity = true;
            rigidBody.AddForce(impactPoint);
            rigidBody.AddTorque(new Vector3(Random.Range(1000000, 2000000), 0, Random.Range(1000000, 2000000)));
            c.GetComponent<MeshCollider>().sharedMesh = meshDouble;
            c.GetComponent<MeshFilter>().sharedMesh = meshDouble;

            forceAccumulation = 0;
        }
    }

    void Update()
    {

    }

    public void CreateFanMesh()
    {
        if (cell.halfEdges.Count > 0)
        {
            meshSunnyside = new Mesh();
            meshDouble = new Mesh();
            meshSunnyside.name = "Chunk " + cell.site.id;
            meshDouble.name = "Chunk " + cell.site.id;

            int numSideVerts = cell.halfEdges.Count + 1;
            int numSideIndices = cell.halfEdges.Count * 3;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            Vector3 vThickness = new Vector3(0, -0.1f, 0);

            vertices.Add(cell.site.ToVector3() - transform.position);
            normals.Add(new Vector3(0, 1, 0));
            uvs.Add(vertices[0]);

            for (int v = 1; v < numSideVerts; v++)
            {
                vertices.Add(cell.halfEdges[v - 1].GetStartPoint().ToVector3() - transform.position);// * 0.98f;
                normals.Add(new Vector3(0, 1, 0));
                uvs.Add(vertices[v]);
                triangles.Add(0);
                triangles.Add(v);
                triangles.Add(v % (numSideVerts - 1) + 1);
            }

            vertices.Add(cell.site.ToVector3() - transform.position + vThickness);
            normals.Add(new Vector3(0, -1, 0));
            uvs.Add(vertices[numSideVerts]);

            for (int v = 1, t = 1; v < numSideVerts; v++, t += 3)
            {
                vertices.Add(vertices[v] + vThickness);
                normals.Add(new Vector3(0, -1, 0));
                uvs.Add(vertices[v + numSideVerts]);
                triangles.Add(v % (numSideVerts - 1) + numSideVerts + 1);
                triangles.Add(v + numSideVerts);
                triangles.Add(numSideVerts);

                triangles.Add(v % (numSideVerts - 1) + 1);
                triangles.Add(v);
                triangles.Add(v + numSideVerts);
                triangles.Add(v + numSideVerts);
                triangles.Add(v % (numSideVerts - 1) + 1 + numSideVerts);
                triangles.Add(v % (numSideVerts - 1) + 1);
            }

            meshSunnyside.vertices = vertices.GetRange(0, numSideVerts).ToArray();
            meshSunnyside.normals = normals.GetRange(0, numSideVerts).ToArray();
            meshSunnyside.uv = uvs.GetRange(0, numSideVerts).ToArray();
            meshSunnyside.triangles = triangles.GetRange(0, numSideIndices).ToArray();
            meshSunnyside.RecalculateBounds();

            meshDouble.vertices = vertices.ToArray();
            meshDouble.normals = normals.ToArray();
            meshDouble.uv = uvs.ToArray();
            meshDouble.triangles = triangles.ToArray();
            meshDouble.RecalculateBounds();

            // not separated at first
            GetComponent<MeshFilter>().sharedMesh = meshSunnyside;

            GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
