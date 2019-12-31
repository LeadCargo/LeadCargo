using System;

namespace FrameworkCore
{
    /// <summary>
    /// Id生成器
    /// 自动生成分布式Id
    /// </summary>
    /// <remarks>
    /// Author:         Peter Hoo
    /// CreateDate:     Jan 1, 2020
    /// </remarks>
    #region 设计思想
    /* ******************************************************************
     * 分布式Id生成器
     * 1. 生成64bit正整数 对应数据库的bigint类型,可做主键
     * 2. 不同的分布式节点生成的Id不能与其他节点生成的ID重复
     * 3. 体现出数据库生成的记录的时间顺序(后生成的ID大于新生成的ID)
     * 4. 支持并发生成,同一节点并发调用时不能重复
     * 结构分离
     * 1. 时间戳(体现生成时间) 41bits
     * 2. 机器节点码(体现分布节节点) 10bits
     * 3. 顺序编码(体现相同时间戳内的顺序) 12bits
     * 4. 保证正数,最高位为0
     * ******************************************************************/
    #endregion 设计思想
    public class IdGenerator
    {
        #region fields
        /// <summary>
        /// 时间基线,该值不可更改
        /// </summary>
        private const long TIMEBASE = 123L;
        /// <summary>
        /// 分部式机器编码
        /// </summary>
        private long _WorkerId = 0L;
        /// <summary>
        /// 顺序码
        /// </summary>
        private long _Sequence = 0L;
        /// <summary>
        /// 顺序码掩码
        /// </summary>
        private const long _SequenceMask = 0x3FFL;
        /// <summary>
        /// 时间戳
        /// </summary>
        private long _Timestamp = 0L;
        /// <summary>
        /// 私有化锁
        /// </summary>
        private static readonly object _Locker = new object();
        /// <summary>
        /// 静态实例
        /// </summary>
        private static IdGenerator _Instance;
        #endregion fields

        #region constructor
        /// <summary>
        /// 构造函数
        /// </summary>
        private IdGenerator(int workid){
            if(workid>=1024 || workid<0)
                throw new ArgumentException("分布式标识仅支持0~1023");
            _WorkerId = workid;
        }
        /// <summary>
        /// 获得运行实例
        /// </summary>
        /// <param name="workid">分布式标识</param>
        /// <returns>生成器实例</returns>
        public static IdGenerator GetInstance(int workid)
        {
            if(_Instance == null)
                lock(_Locker)
                    if(_Instance == null)
                        _Instance = new IdGenerator(workid);
            return _Instance;
        }
        #endregion constructor

        #region properties
        /// <summary>
        /// 分布式节点编码
        /// </summary>
        public int WorkId
        {
            get{return (int)_WorkerId;}
            private set{}
        }
        #endregion properties

        #region methods
        /// <summary>
        /// 获取下一个ID
        /// </summary>
        /// <returns>Id</returns>
        public long NextId()
        {
            var currentTimestap = Timestamp();
            // 检查当前时间戳
            if(currentTimestap>_Timestamp){
                _Timestamp = currentTimestap;
                _Sequence = 0;
            }
            // 检查序号是否超编
            if((_Sequence++&_SequenceMask)==0){
                _Timestamp++;
                _Sequence =0L;
            }
            // 返回结果
            return _Timestamp<<22|_WorkerId<<12|_Sequence;

        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns>时间戳数值</returns>
        private long Timestamp() =>
            (DateTime.UtcNow.Ticks -TIMEBASE)>>14;
        #endregion methods
    }
}