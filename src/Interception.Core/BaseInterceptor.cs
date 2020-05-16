﻿using Interception.Attributes;
using Interception.Core.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Interception.Core
{
    /// <summary>
    /// base interceptor
    /// </summary>
    public abstract class BaseInterceptor : IInterceptor
    {
        private IMethodFinder _methodFinder = new MethodFinder();

        private object[] _parameters;

        private object _this;

        private int _mdToken;

        private long _moduleVersionPtr;

        private string _key;

        public void SetThis(object _this)
        {
            //Console.WriteLine($"SetThis {_this}");
            this._this = _this;
        }

        public object GetThis()
        {
            return _this;
        }

        public void AddParameter(int num, object value)
        {
            //Console.WriteLine($"AddParameter {num} {value}");
            _parameters[num] = value;
        }

        public void SetMdToken(int mdToken)
        {
            //Console.WriteLine($"SetMdToken {mdToken}");
            _mdToken = mdToken;
        }

        public int GetMdToken()
        {
            return _mdToken;
        }

        public void SetModuleVersionPtr(long moduleVersionPtr)
        {
            //Console.WriteLine($"SetModuleVersionPtr {moduleVersionPtr}");
            _moduleVersionPtr = moduleVersionPtr;
        }

        public long GetModuleVersionPtr()
        {
            return _moduleVersionPtr;
        }

        public object GetParameter(int num)
        {
            return _parameters[num];
        }

        public void UpdateParameter(int num, object value)
        {
            _parameters[num] = value;
        }

        public void SetArgumentNumber(int number)
        {
            //Console.WriteLine($"SetArgumentNumber {number}");
            _parameters = new object[number];
        }

        public void SetParameters(object[] parameters)
        {
            _parameters = parameters;
        }

        public object[] GetParameters()
        {
            return _parameters;
        }

        public void SetKey(string key)
        {
            //Console.WriteLine($"SetKey {key}");
            _key = key;
        }

        public string GetKey()
        {
            return _key;
        }

        public abstract int Priority { get; }

        public virtual object Execute()
        {
            Validate();
            var method = FindMethod();
            var isAsync = method.IsReturnTypeTask();

            if (!isAsync)
            {
                return ExecuteSyncInternal();
            }
            else if (!method.IsReturnTypeTaskWithResult())
            {
                return ExecuteAsyncInternal();
            }
            else
            {
                return ExecuteAsyncWithResultInternal();
            }
        }

        public virtual void ExecuteBefore()
        { 
        }

        public virtual void ExecuteAfter(object result, Exception exception)
        { 
        }

        protected virtual MethodInfo FindMethod()
        {
            return (MethodInfo)_methodFinder.FindMethod(_mdToken, _moduleVersionPtr);
        }

        #region internal
        protected object ExecuteSyncInternal()
        {
            var method = FindMethod();

            try
            {
                ExecuteBefore();
                var result = method.Invoke(_this, _parameters);
                ExecuteAfter(result, null);
                return result;
            }
            catch (Exception ex)
            {
                ExecuteAfter(null, ex);
                throw;
            }
        }

        protected async Task ExecuteAsyncInternal()
        {
            //Console.WriteLine("ExecuteAsyncInternal");
            var method = FindMethod();

            try
            {
                ExecuteBefore();
                var result = (Task)method.Invoke(_this, _parameters);
                await result;
                ExecuteAfter(result, null);
            }
            catch (Exception ex)
            {
                ExecuteAfter(null, ex);
                throw;
            }
        }

        internal static readonly ModuleBuilder Module;

        static BaseInterceptor()
        {
            var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Interception"), AssemblyBuilderAccess.Run);
            Module = asm.DefineDynamicModule("DynamicModule");
        }

        protected object ExecuteAsyncWithResultInternal()
        {
            var method = FindMethod();

            var underilyingType = ((TypeInfo)method.ReturnType).GenericTypeArguments[0];

            var createExecutionDelegateGeneric = typeof(BaseInterceptor).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => m.Name == nameof(CreateExecutionDelegate)).First();
            var createExecutionDelegate = createExecutionDelegateGeneric.MakeGenericMethod(underilyingType);
            var executionDelegate = createExecutionDelegate.Invoke(this, new object[] { });

            var executeWithMetricsGeneric = typeof(BaseInterceptor).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Where(m => m.Name == nameof(ExecuteWithMetrics)).First();
            var executeWithMetrics = executeWithMetricsGeneric.MakeGenericMethod(underilyingType);

            return executeWithMetrics.Invoke(this, new object[] { executionDelegate });
        }

        private async Task<T> ExecuteWithMetrics<T>(Delegate funcDelegate)
        {
            try
            {
                ExecuteBefore();

                var func = (Func<Task<T>>)funcDelegate;
                var result = await func.Invoke();

                ExecuteAfter(result, null);

                return result;
            }
            catch (Exception ex)
            {
                ExecuteAfter(null, ex);
                throw;
            }
        }

        private Delegate CreateExecutionDelegate<T>()
        {
            var method = FindMethod();

            Func<Task<T>> func = () => {
                return (Task<T>)method.Invoke(_this, _parameters);
            };

            return func;
        }

        protected void Validate()
        {
            var method = FindMethod();
            foreach (var p in method.GetParameters())
            {
                var validationAttributes = p.GetCustomAttributes<ParameterValidationAttribute>();

                foreach (var validationAttribute in validationAttributes)
                {
                    validationAttribute.Validate(_parameters[p.Position]);
                }
            }
        }
        #endregion
    }
}
