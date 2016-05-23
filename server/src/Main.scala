import java.io._
import java.net.{Socket, ServerSocket}
import java.nio.{ByteBuffer, ByteOrder}

import scala.collection.mutable
import scala.math.Ordering

object Main {
  val netMesh = NetMeshLoader.load("..\\NetMesh.data").get
  def main(args: Array[String]): Unit = {
//    netMesh.calcPath(Vector3(0, 0, 0), Vector3(40, 0, 40))
//    println(netMesh.corners.length)
    try {
      val server = new ServerSocket(12345)
      println("Start listen on: %d" format server.getLocalPort)
      while (true) {
        new ServerThread(server.accept(), MessageHandler).start()
      }
      server.close()
    }
    catch {
      case e: Exception => e.printStackTrace()
    }
  }

  def MessageHandler(out: DataOutputStream, msg: Any) = {
    println("Handle Message: %s" format msg)
    msg match {
      case ok: Boolean => {
        if (ok) {
          netMesh.calcPath(Vector3(0, 0, 0), Vector3(40f, 0, 40f))
          println("Arrived: %s" format netMesh.arrived)
          var buffer: ByteBuffer = null
          if (netMesh.arrived) {
            val clen = 1 + 4 + netMesh.corners.length * 3 * 4
            buffer = ByteBuffer.allocate(4 + clen)
            buffer.order(ByteOrder.LITTLE_ENDIAN)
            buffer.putInt(clen)
                  .put(byte2Byte(0x01))
                  .putInt(netMesh.corners.length)
            netMesh.corners.foreach(_.foreach(f => buffer.putFloat(f)))
            buffer.order(ByteOrder.BIG_ENDIAN)
          }
          else {
            val clen = 1
            buffer = ByteBuffer.allocate(4 + clen)
            buffer.order(ByteOrder.LITTLE_ENDIAN)
            buffer.putInt(clen).put(byte2Byte(0x00))
            buffer.order(ByteOrder.BIG_ENDIAN)
          }
          buffer.rewind()
          println("send: %d bytes" format buffer.capacity())
          val bytes = new Array[Byte](buffer.capacity())
          buffer.get(bytes, 0, bytes.length)
          out.write(bytes)
          out.flush()
        }
      }
    }
  }

  def Test(): Unit = {
    val vertices = List(
      Vector3(0, 0, 0),
      Vector3(0, 0, 1),
      Vector3(1, 0, 1),
      Vector3(1, 0, 0)
    )
    val indices = List(
      0, 1, 2,
      0, 2, 3
    )
    val layers = List(1, 1)

    val netMesh: NetMesh = NetMesh(vertices, indices, layers)
    /*
    println(netMesh.triangles(0).neighbours.length)
    println(netMesh.triangles(0).min)
    println(netMesh.triangles(0).max)
    println(netMesh.triangles(0).contains(Vector3(0.5f, -0.1f, 0.5f), 0.1f))
    */
    val start = System.currentTimeMillis()
    netMesh.calcPath(Vector3(0, 0, 1), Vector3(0.8f, 0, 0))
    println("Time: " + (System.currentTimeMillis() - start) + "ms")
    println("Arrived: " + netMesh.arrived)
    println("Cost: " + netMesh.cost)
    println("Corners: " + netMesh.corners.length)
    netMesh.corners.foreach(c => println(c))
  }
}

class Vector3(val x: Float, val y: Float, val z: Float) {
  private var sm: Option[Float] = None
  private var m: Option[Float] = None
  private var n: Option[Vector3] = None
  override def toString = s"( x: $x, y: $y, z: $z )"
  def ==(v: Vector3) = (x, y, z) == (v.x, v.y, v.z)
  def !=(v: Vector3) = (x, y, z) != (v.x, v.y, v.z)
  def <=(v: Vector3) = x <= v.x && y <= v.y && z <= v.z
  def >=(v: Vector3) = x >= v.x && y >= v.y && z >= v.z
  def +(v: Vector3) = Vector3(x + v.x, y + v.y, z + v.z)
  def -(v: Vector3) = Vector3(x - v.x, y - v.y, z - v.z)
  def *[T <% Float](f: T) = Vector3(x * f, y * f, z * f)
  def /[T <% Float](f: T) = Vector3(x / f, y / f, z / f)
  def *(v: Vector3) = Vector3.dot(this, v)
  def X(v: Vector3) = Vector3.cross(this, v)
  def sqrtMagnitude = sm.getOrElse { sm = Some(x * x + y * y + z * z); sm.get }
  def magnitude = m.getOrElse { m = Some(Math.sqrt(sqrtMagnitude).toFloat); m.get }
  def normalized = n.getOrElse { n = Some(this / magnitude); n.get }
}

object Vector3 {
  def apply(x: Float, y: Float, z: Float) = new Vector3(x, y, z)
  def dist(p1: Vector3, p2: Vector3) = (p1 - p2).magnitude
  def sqrtDist(p1: Vector3, p2: Vector3) = (p1 - p2).sqrtMagnitude
  def manhattanDist(p1: Vector3, p2: Vector3) = (p1 - p2).foldLeft(0f)((r, f) => r + math.abs(f))
  def lerp(v1: Vector3, v2: Vector3, t: Float) = v1 * (1 - t) + v2 * t
  def dot(v1: Vector3, v2: Vector3) = v1.x * v2.x + v1.y * v2.y + v1.z * v2.z
  def cross(v1: Vector3, v2: Vector3) = Vector3(
    v1.y * v2.z - v1.z * v2.y,
    v1.z * v2.x - v1.x * v2.z,
    v1.x * v2.y - v1.y * v2.x
  )
  implicit def Vector3ToList(v: Vector3): List[Float] = List(v.x, v.y, v.z)
  implicit def ListToVector3(list: List[Float]): Vector3 = Vector3(list(0), list(1), list(2))
  def min(points: List[Vector3]) = 0 to 2 map(i => points.min(Ordering.by[Vector3, Float](_(i)))(i)) toList
  def max(points: List[Vector3]) = 0 to 2 map(i => points.max(Ordering.by[Vector3, Float](_(i)))(i)) toList
}

class Edge(point0: Vector3, point1: Vector3) {
  val uniqueId = -1
  val points: List[Vector3] = List(point0, point1)
  val vector: Vector3 = point1 - point0
  lazy val center: Vector3 = Vector3.lerp(point0, point1, 0.5f)
  def ==(e: Edge) = points == e.points
  def !=(e: Edge) = points != e.points
  override def toString = s"[ p0: $point0, p1: $point1 ]"
  // 判断点是否在线段上
  def contains(p: Vector3): Boolean = {
    Vector3.cross(p - point0, vector).sqrtMagnitude < 0.00001f &&
    p.x >= List(point0.x, point1.x).min &&
    p.y >= List(point0.y, point1.y).min &&
    p.z >= List(point0.z, point1.z).min &&
    p.x <= List(point0.x, point1.x).max &&
    p.y <= List(point0.y, point1.y).max &&
    p.z <= List(point0.z, point1.z).max
  }
}

object Edge {
  def dist(e1: Edge, e2: Edge) = Vector3.dist(e1.center, e2.center)
  def manhattanDist(e1: Edge, e2: Edge) = Vector3.manhattanDist(e1.center, e2.center)
}

class IndexEdge(index0: Tuple2[Int, Vector3], index1: Tuple2[Int, Vector3]) extends Edge(index0._2, index1._2) {
  override val uniqueId = (Math.max(index0._1, index1._1).toString() + Math.min(index0._1, index1._1)).toInt
  override def ==(e: Edge) = uniqueId == e.uniqueId
  override def !=(e: Edge) = uniqueId == e.uniqueId
}

class Triangle(edge0: Edge, edge1: Edge, edge2: Edge, triangleLayer: Int) {
  val edges = List(edge0, edge1, edge2)
  val points = edges.map(_.points(0))
  lazy val min: Vector3 = Vector3.min(points)
  lazy val max: Vector3 = Vector3.max(points)
  lazy val center: Vector3 = points.foldLeft(Vector3(0, 0, 0))((p, each) => p + each) / points.length
  val neighbours: mutable.Queue[NetMeshPathStep] = new mutable.Queue()
  val layer = triangleLayer
  override def toString = "{ p0: %s, p1: %s, p2: %s }" format (points(0), points(1), points(2))
  // 判断点是否在三角形内(包括边)
  // stepHeight: 容错高度
  def contains(p: Vector3, stepHeight: Float = 0f): Boolean = {
    edges.filter(e => !e.contains(p)).forall(e => {
      Vector3.cross(
        Vector3(e.vector.x, 0, e.vector.z),
        Vector3(p.x - e.points(0).x, 0, p.z - e.points(0).z)
      ).y >= 0
    }) &&
    p.y >= min.y - stepHeight &&
    p.y <= max.y + stepHeight
  }
}

/**
 * 穿出边，相邻三角形，穿入边
 * @param out
 * @param in
 * @param next
 */
class NetMeshPathStep(val out: Edge, val next: Triangle, val in: Edge) {

}

class NetMesh(val triangles: List[Triangle]) {
  // 构建四叉树，用以快速查找某个点所在的三角形
  val root: Tree4Node = new Tree4Node(
    Vector3(Float.MinValue, Float.MinValue, Float.MinValue),
    Vector3(Float.MaxValue, Float.MaxValue, Float.MaxValue)
  )
  triangles.foreach(t => root.insert(t))
  var arrived = false
  var cost = 0f;
  var corners: List[Vector3] = null
  // A*网格寻路(g(n)为边中心点距离，h(n)为欧几里得距离)
  // 采用拐点法生成路径点(calcCorners)
  // 待优化
  def calcPath(fromPoint: Vector3, toPoint: Vector3) = {
    val start = System.currentTimeMillis()
    val from = root.search(fromPoint, NetMesh.stepHeight)
    val to = root.search(toPoint, NetMesh.stepHeight)
    val middle = System.currentTimeMillis()
    println("1. locate point: %dms" format middle - start)
    if (from.isDefined && to.isDefined) {
      val dist: mutable.Map[Triangle, Float] = mutable.Map()
      val path: mutable.Map[Triangle, List[NetMeshPathStep]] = mutable.Map()
      val open: mutable.PriorityQueue[Triangle] = mutable.PriorityQueue[Triangle]()(Ordering.by[Triangle, Float](t => dist(t) + Vector3.dist(path(t).last.in.center, toPoint)).reverse)
      val close: mutable.HashSet[Triangle] = mutable.HashSet()
      dist += from.get -> 0f
      path += from.get -> List()
      open.enqueue(from.get)
      while(!arrived && open.size > 0) {
        val t = open.dequeue()
        close.add(t)
        if (t == to.get) {
          val steps = path(to.get)
          if (steps.length == 0) cost = Vector3.dist(fromPoint, toPoint)
          else cost = dist(to.get) + Vector3.dist(steps.last.in.center, toPoint)
          val end = System.currentTimeMillis()
          println("2. A* pathfinding: %dms" format end - middle)
          //corners = fromPoint :: steps.map(step => step.next.center) ::: List(toPoint)
          corners = calcCorners(fromPoint, toPoint, steps)
          println("3. Corner calculation: %dms" format System.currentTimeMillis() - end)
          arrived = true
        }
        else {
          val tdist = dist(t)
          val tpath = path(t)
          val neighbours = t.neighbours.filter(n => !open.exists(_ == n.next) && !close.contains(n.next))
          if (neighbours.size > 0) {
            if (tpath.size == 0) {
              neighbours.foreach(neighbour => {
                dist.update(neighbour.next, tdist + Vector3.dist(fromPoint, neighbour.out.center))
                path.update(neighbour.next, tpath ::: List(neighbour))
                open.enqueue(neighbour.next)
              })
            }
            else {
              val last = tpath.last
              neighbours.foreach(neighbour => {
                dist.update(neighbour.next, tdist + Edge.dist(last.in, neighbour.out))
                path.update(neighbour.next, tpath ::: List(neighbour))
                open.enqueue(neighbour.next)
              })
            }
          }
        }
      }
    }
  }
  def calcCorners(from: Vector3, to: Vector3, steps: List[NetMeshPathStep]) = {
    val corners = mutable.Queue(from)
    var current = from
    var joint: List[(Vector3, NetMeshPathStep)] = null
    var index = 0
    while (index < steps.length) {
      val step = steps(index)
      if (joint == null) {
        joint = step.out.points.zip(List(step, step))
      }
      else {
        val vstep: Seq[Vector3] = step.out.points.map(_ - current)
        val cross = vstep.flatMap(v => joint.map(p => p._1 - current).map(ve => Vector3.cross(ve, v).y))
        if (cross.forall(b => b <= 0)) {
          corners.enqueue(joint(0)._1)
          current = joint(0)._1
          index = steps.indexOf(joint(0)._2)
          joint = null
        }
        else if (cross.forall(b => b >= 0)) {
          corners.enqueue(joint(1)._1)
          current = joint(1)._1
          index = steps.indexOf(joint(1)._2)
          joint = null
        }
        else {
          if (cross(0) >= 0 && cross(1) <= 0) {
            joint = List(step.out.points(0) -> step, joint(1))
          }
          if (cross(2) >= 0 && cross(3) <= 0) {
            joint = List(joint(0), step.out.points(1) -> step)
          }
        }
      }
      index += 1
    }
    corners.enqueue(to)
    corners.toList
  }
}

object NetMesh {
  val stepHeight = 0.5f
  def apply(vertices: List[Vector3], indices: List[Int], layers: List[Int]): NetMesh = {
    if (vertices.groupBy(_.toList).exists({case (k, list) => list.length > 1})) {
      throw new IllegalArgumentException("顶点集中不能包含重复的顶点")
    }
    val map: mutable.HashMap[Int, mutable.Queue[Triangle]] = mutable.HashMap()
    new NetMesh(indices.grouped(3).toList.zip(layers).map({case (indice3: List[Int], layer: Int) => {
      val vertice3 = indice3.map(indice => vertices(indice))
      new Triangle(
        new IndexEdge(indice3(0) -> vertice3(0), indice3(1) -> vertice3(1)),
        new IndexEdge(indice3(1) -> vertice3(1), indice3(2) -> vertice3(2)),
        new IndexEdge(indice3(2) -> vertice3(2), indice3(0) -> vertice3(0)),
        layer) {
        edges.foreach(e => {
          val key = e.uniqueId
          if (!map.contains(key)) map += key -> new mutable.Queue[Triangle]()
          map(key).foreach(t => {
            t.edges.find(_ == e).foreach(te => {
              neighbours.enqueue(new NetMeshPathStep(e, t, te))
              t.neighbours.enqueue(new NetMeshPathStep(te, this, e))
            })
          })
          map(key).enqueue(this)
        })
      }
    }}))
  }
}

/**
 * xz平面四叉树节点（y == 0）
 * @param min
 * @param max
 */
class Tree4Node(min: Vector3, max: Vector3) {
  private var leaf = true
  private val triangles: mutable.Queue[Triangle] = new mutable.Queue[Triangle]()
  private val children: mutable.ArraySeq[Tree4Node] = new mutable.ArraySeq[Tree4Node](4)
  private def contains(p: Vector3) = min <= p && p <= max
  private def contains(t: Triangle) = min <= t.min && t.max <= max
  private def partition() = {
    val center = triangles.foldLeft(Vector3(0, 0, 0))((p, each) => p + each.center) / triangles.length
    val areas = List(
      min -> Vector3(center.x, max.y, center.z),
      Vector3(center.x, min.y, center.z) -> max,
      Vector3(center.x, min.y, min.z) -> Vector3(max.x, max.y, center.z),
      Vector3(min.x, min.y, center.z) -> Vector3(center.x, max.y, max.z)
    )
    0 to 3 foreach(i => children.update(i, new Tree4Node(areas(i)._1, areas(i)._2)))
    triangles.filterNot(t => children.exists(child => child.insert(t)))
  }
  def insert(t: Triangle): Boolean = {
    var result = false;
    if (contains(t)) {
      if (leaf) {
        triangles.enqueue(t)
        if (triangles.length > Tree4Node.nodeMaxHoldNums) {
          partition()
          leaf = false
        }
        result = true
      }
      else {
        result = children.exists(child => child.insert(t))
        if (!result) triangles.enqueue(t)
      }
    }
    result
  }
  def search(p: Vector3, stepHeight: Float): Option[Triangle] = {
    var result: Option[Triangle] = None
    if (contains(p)) {
      result = triangles.find(t => t.contains(p, stepHeight))
      if (!result.isDefined) {
        children.find(child => { result = child.search(p, stepHeight); result.isDefined })
      }
    }
    result
  }
}

object Tree4Node {
  val nodeMaxHoldNums = 16
}

object NetMeshLoader {
  def load(path: String): Option[NetMesh] = {
    val file = new File(path)
    if (file.exists()) {
      val stream = new FileInputStream(file)
      val bytes = new Array[Byte](stream.available())
      stream.read(bytes, 0, bytes.length)
      val buffer = ByteBuffer.allocate(bytes.length)
      buffer.order(ByteOrder.BIG_ENDIAN)
      buffer.put(bytes)
      buffer.order(ByteOrder.LITTLE_ENDIAN)
      buffer.rewind()
      val vcount = buffer.getInt()
      val vertices = (1 to vcount).map(_ => Vector3(buffer.getFloat(), buffer.getFloat(), buffer.getFloat())).toList
      val tcount = buffer.getInt()
      val indices = (1 to 3 * tcount).map(_ => buffer.getInt()).toList
      val layers = (1 to tcount).map(_ => buffer.getInt()).toList
      buffer.clear()
      return Some(NetMesh(vertices, indices, layers))
    }
    None
  }
}

class ServerThread(socket: Socket, MessageHandler: (DataOutputStream, Any) => Unit) extends Thread("ServerThread") {
  override def run(): Unit = {
    try {
      println("New socket connected in --- ip: %s port: %d".format(socket.getInetAddress, socket.getPort))
      val in = new DataInputStream(socket.getInputStream)
      val out = new DataOutputStream(socket.getOutputStream)
      while (socket.isConnected) {
        if (in.available() > 0) {
          MessageHandler(out, in.readBoolean())
        }
        Thread.sleep(100)
      }
      in.close()
      out.close()
      socket.close()
    }
    catch {
      case e: Exception => e.printStackTrace()
    }
  }
}