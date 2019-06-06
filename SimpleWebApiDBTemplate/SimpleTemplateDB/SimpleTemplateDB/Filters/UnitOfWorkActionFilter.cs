using Common.UOW;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleTemplateDB.Filters
{
    public class UnitOfWorkActionFilter : ActionFilterAttribute
    {
        public IUnitOfWork UnitOfWork { get; set; }

        public UnitOfWorkActionFilter(IUnitOfWork _uow)
        {
            UnitOfWork = _uow;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            UnitOfWork.BeginTransaction();
        }

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception == null)
            {
                // commit if no exceptions
                UnitOfWork.Commit();
            }
            else
            {
                // rollback if exception
                UnitOfWork.Rollback();
            }
        }
    }
}
