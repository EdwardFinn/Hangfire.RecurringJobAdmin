using Hangfire.Annotations;
using Hangfire.Dashboard;
using Hangfire.RecurringJobAdmin.Core;
using Hangfire.RecurringJobAdmin.Models;
using Hangfire.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.RecurringJobAdmin.Pages
{
    internal sealed class ChangeJobDispatcher : IDashboardDispatcher
    {
        private readonly IStorageConnection _connection;
        public ChangeJobDispatcher()
        {

            _connection = JobStorage.Current.GetConnection();
        }


        public async Task Dispatch([NotNull] DashboardContext context)
        {
            var response = new Response() { Status = true };

            
            string jobId = context.Request.GetQuery("Id");
            string jobCron = context.Request.GetQuery("Cron");
            string jobClass = context.Request.GetQuery("Class");
            string jobMethod = context.Request.GetQuery("Method");
            string jobQueue = context.Request.GetQuery("Queue");

            if (!Utility.IsValidSchedule(jobCron))
            {
                response.Status = false;
                response.Message = "Invalid CRON";

                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

                return;
            }
            Type classType = GetType(jobClass);
            MethodInfo methodInfo = classType.GetMethod(jobMethod);

            var job = new Hangfire.Common.Job(classType, methodInfo);
            var manager = new RecurringJobManager(context.Storage);

            //manager.AddOrUpdate(job.Id, () => ReflectionHelper.InvokeVoidMethod(job.Class, job.Method), job.Cron, TimeZoneInfo.Utc, job.Queue);
            
            
            manager.AddOrUpdate(jobId, job, jobCron, new RecurringJobOptions() { TimeZone = TimeZoneInfo.Utc, QueueName = jobQueue });


            context.Response.StatusCode = (int)HttpStatusCode.OK;

            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));

        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
