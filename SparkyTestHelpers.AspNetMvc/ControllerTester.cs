﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SparkyTestHelpers.AspNetMvc
{
    /// <summary>
    /// ASP.NET Core MVC Controller tester.
    /// </summary>
    /// <typeparam name="TController">The controller type.</typeparam>
    public class ControllerTester<TController> where TController : Controller
    {
        private readonly TController _controller;

        /// <summary>
        /// Creates new <see cref="ControllerTester{TController}"/> instance.
        /// </summary>
        /// <param name="controller">The controller.</param>
        public ControllerTester(TController controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// Creates a new <see cref="ControllerActionTester"/> instance.
        /// </summary>
        /// <param name="expression">Expression to specify the controller action to be tested.</param>
        /// <returns>New <see cref="ControllerActionTester"/> instance.</returns>
        public ControllerActionTester Action(Expression<Func<TController, Func<ActionResult>>> expression)
        {
            var func = expression.Compile();

            return new ControllerActionTester(_controller, func(_controller));
        }

        /// <summary>
        /// Creates a new <see cref="ControllerActionTester"/> instance for an async action.
        /// </summary>
        /// <param name="expression">Expression to specify the async controller action to be tested.</param>
        /// <returns>New <see cref="ControllerActionTester"/> instance.</returns>
        public ControllerActionTester Action(Expression<Func<TController, Func<Task<ActionResult>>>> expression)
        {
            Func<TController, Func<Task<ActionResult>>> funcWithTask = expression.Compile();

            Func<ActionResult> func = () =>
            {
                Task<ActionResult> task = funcWithTask(_controller)();
                task.Wait();
                return task.Result;
            };

            return new ControllerActionTester(_controller, func);
        }
    }
}
