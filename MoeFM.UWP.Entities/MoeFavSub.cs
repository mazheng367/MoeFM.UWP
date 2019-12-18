 // ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities
{
    public class MoeFavSub<T>
    {
        /// <summary>
        ///     收藏id号，不同大类的对象的fav_id是可重叠的
        /// </summary>
        public long fav_id { get; set; }

        /// <summary>
        ///     被收藏的对象的id
        /// </summary>
        public long fav_obj_id { get; set; }

        /// <summary>
        ///     被收藏的对象的类型
        /// </summary>
        public string fav_obj_type { get; set; }

        /// <summary>
        ///     执行收藏的用户
        /// </summary>
        public long fav_uid { get; set; }

        /// <summary>
        ///     收藏的时间
        /// </summary>
        public double fav_date { get; set; }

        /// <summary>
        ///     收藏类型
        /// </summary>
        public int fav_type { get; set; }

        /// <summary>
        ///     被收藏的对象，本例中应为条目对象；在某些场景中此项可能不存在
        /// </summary>
        public T obj { get; set; }
    }
}