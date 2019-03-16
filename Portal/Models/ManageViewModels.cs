using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace Portal.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "{0} می بایست حداقل  {2} کاراکتر باشد.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور جدید")]
        [Compare("NewPassword", ErrorMessage = "رمز عبور با تکرار آن یکسان نیست")]
        public string ConfirmPassword { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور قبلی")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} می بایست حداقل  {2} کاراکتر باشد.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور جدید")]
        [Compare("NewPassword", ErrorMessage = "رمز عبور با تکرار آن یکسان نیست")]
        public string ConfirmPassword { get; set; }
    }

    public class User_ChangePasswordViewModel
    {
        public User_ChangePasswordViewModel()
        {

        }
        public User_ChangePasswordViewModel(string userId)
        {
            this.userId = userId;
        }

        [DataType(DataType.Text)]
        [Display(Name = "شناسه کاربر")]
        public string userId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "{0} می بایست حداقل  {2} کاراکتر باشد.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور جدید")]
        [Compare("NewPassword", ErrorMessage = "رمز عبور با تکرار آن یکسان نیست")]
        public string ConfirmPassword { get; set; }

        public bool SuccessfullySaved { get; set; }
    }

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    }
    public class Bulks_EditViewModel
    {
        public Bulks_EditViewModel()
        {
            this.status = SharedLibrary.MessageHandler.BulkStatus.Enabled;
        }

        public Bulks_EditViewModel(Nullable<int> id)
        {
            this.status = SharedLibrary.MessageHandler.BulkStatus.Enabled;
            if (!id.HasValue) return;
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var bulk = portal.Bulks.Where(o => o.Id == id).FirstOrDefault();
                if (bulk != null)
                {
                    this.BulkName = bulk.bulkName;
                    if (bulk.DateCreated.HasValue)
                        this.DateCreated = bulk.DateCreated.Value.ToString("yyyy/MM/dd HH:mm:ss");
                    this.endTime = bulk.endTime;
                    this.BulkId = id;
                    this.message = bulk.message;

                    this.PersianDateCreated = bulk.PersianDateCreated;
                    this.readSize = bulk.readSize.HasValue ? bulk.readSize.Value : 0;
                    this.retryCount = bulk.retryCount.HasValue ? bulk.retryCount.Value : 0;
                    this.retryIntervalInSeconds = bulk.retryIntervalInSeconds.HasValue ? bulk.retryIntervalInSeconds.Value : 0;
                    this.service = bulk.ServiceId.ToString();
                    this.startTime = bulk.startTime;
                    this.status = (SharedLibrary.MessageHandler.BulkStatus)bulk.status;
                    this.TotalFailed = bulk.TotalFailed.HasValue ? bulk.TotalFailed.Value : 0;
                    this.TotalMessages = bulk.TotalMessages.HasValue ? bulk.TotalMessages.Value : 0;
                    this.TotalRetryCount = bulk.TotalRetry.HasValue ? bulk.TotalRetry.Value : 0;
                    this.TotalRetryCountUnique = bulk.TotalRetryUnique.HasValue ? bulk.TotalRetryUnique.Value : 0;
                    this.TotalSuccessfullySent = bulk.TotalSuccessfullySent.HasValue ? bulk.TotalSuccessfullySent.Value : 0;
                    this.tps = bulk.tps;

                }
            }


        }

        [Display(Name = "شناسه Bulk")]
        public Nullable<int> BulkId { get; set; }

        [Display(Name = "عنوان Bulk"), Required(ErrorMessage = "عنوان مشخص نشده است")]
        public string BulkName { get; set; }

        [Display(Name = "سرویس"), Required(ErrorMessage = "سرویس مشخص نشده است")]
        public string service { get; set; }

        [Display(Name = "tps"), Required(ErrorMessage = "tps مشخص نشده است"), Range(1, 1000, ErrorMessage = "مقدار tps بین 1 تا 1000")]
        public int tps { get; set; }

        [DataType(DataType.DateTime), Required(ErrorMessage = "زمان شروع مشخص نشده است"), Display(Name = "زمان شروع")]
        public Nullable<DateTime> startTime { get; set; }

        [DataType(DataType.DateTime), Required(ErrorMessage = "زمان پایان مشخص نشده است"), Display(Name = "زمان پایان")]
        public Nullable<DateTime> endTime { get; set; }

        [DataType(DataType.Text), Required(ErrorMessage = "پیام مشخص نشده است"), Display(Name = "پیام"), StringLength(4000, ErrorMessage = "حداکثر تعداد کاراکتر مجاز 4000")]
        public string message { get; set; }


        [Display(Name = "Read Size")]
        public int readSize { get; set; }


        [Display(Name = "حداکثر تعداد تلاش مجدد"), Range(0, 20, ErrorMessage = "مقدار قابل قبول 0 تا 20")]
        public int retryCount { get; set; }

        [Display(Name = "فاصله زمانی برای تلاش مجدد(ثانیه)"), Range(0, 3600, ErrorMessage = "مقدار قابل قبول 0 تا 3600")]
        public int retryIntervalInSeconds { get; set; }

        [Display(Name = "تعداد کل پیامها"), Editable(false)]
        public int TotalMessages { get; set; }

        [Display(Name = "تعداد ارسالهای موفق"), Editable(false)]
        public int TotalSuccessfullySent { get; set; }

        [Display(Name = "مجموع تلاشهای مجدد"), Editable(false)]
        public int TotalRetryCount { get; set; }

        [Display(Name = "تعداد تلاشهای مجدد منحصر بفرد"), Editable(false)]
        public int TotalRetryCountUnique { get; set; }


        [Display(Name = "تعداد Delivery"), Editable(false)]
        public int TotalDelivery { get; set; }

        [Display(Name = "تعداد ارسالهای ناموفق"), Editable(false)]
        public int TotalFailed { get; set; }

        [Display(Name = "Bulk File")]
        public string bulkFile { get; set; }

        [Display(Name = "وضعیت"), EnumDataType(typeof(SharedLibrary.MessageHandler.BulkStatus)), Editable(false)
            , DefaultValue(SharedLibrary.MessageHandler.BulkStatus.Enabled)]
        public SharedLibrary.MessageHandler.BulkStatus status { get; set; }

        [Display(Name = "زمان ثبت"), DataType(DataType.DateTime), Editable(false)]
        public string DateCreated { get; set; }

        [Display(Name = "زمان ثبت شمسی"), DataType(DataType.DateTime), Editable(false)]
        public string PersianDateCreated { get; set; }
    }

    public class Bulks_NumbersViewModel
    {
        public Bulks_NumbersViewModel()
        {
            deleteOldData = true;
        }

        public Bulks_NumbersViewModel(Nullable<int> id)
        {
            deleteOldData = true;
            if (!id.HasValue) return;
            using (var portal = new SharedLibrary.Models.PortalEntities())
            {
                var bulk = portal.Bulks.Where(o => o.Id == id).FirstOrDefault();
                if (bulk != null)
                {
                    this.BulkName = bulk.bulkName;
                    this.BulkId = id;
                    var entryService = portal.vw_servicesServicesInfo.FirstOrDefault(o => o.Id == bulk.ServiceId);
                    if (entryService != null)
                    {
                        this.serviceCodeAndName = entryService.ServiceCode + "(" + entryService.Name + ")";
                    }
                }
            }
        }
        [Display(Name = "شناسه Bulk"), Editable(false)]
        public Nullable<int> BulkId { get; set; }

        [Display(Name = "عنوان Bulk"), Editable(false)]
        public string BulkName { get; set; }

        [Display(Name = "سرویس"), Editable(false)]
        public string serviceCodeAndName { get; set; }

        [Display(Name = "Bulk File Type")]
        public string bulkFileType { get; set; }

        [Display(Name = "File")]
        public string filePath { get; set; }

        [FileExtensions(".txt,.csv", fileSizeInBytes = 100 * 1024 * 1024, ErrorMessage = "نوع فایلهای معتبر .txt و .csv می باشد و حداکثر حجم فایل 100 مگا بایت است")]
        public HttpPostedFile fileUpload { get; set; }

        [Display(Name = "نام جدول لیست شماره ها")]
        public string sqlTableName { get; set; }

        [Display(Name = "لیست شماره ها")]
        public string bulkList { get; set; }

        [Display(Name = "شماره های موبایل با 98 آغاز می شوند")]
        public bool mobileNumberStartsWith98 { get; set; }

        [Display(Name = "ذخیره")]
        public bool save { get; set; }

        [Display(Name = "نوع جدول مقصد")]
        public string destinationType { get; set; }

        [Display(Name = "نام جدول مقصد")]
        public string destinationSqlTableName { get; set; }
        [Display(Name = "حذف داده های قدیمی"), DefaultValue(true)]
        public bool deleteOldData { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FileExtensionsAttribute : ValidationAttribute
    {
        private List<string> AllowedExtensions { get; set; }

        public FileExtensionsAttribute(string fileExtensions)
        {
            AllowedExtensions = fileExtensions.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public long fileSizeInBytes { get; set; }
        public override bool IsValid(object value)
        {
            HttpPostedFileBase file = value as HttpPostedFileBase;

            if (file != null)
            {
                var fileName = file.FileName;

                if (!AllowedExtensions.Any(y => fileName.EndsWith(y))) return false;
                if (file.ContentLength > fileSizeInBytes) return false;
            }

            return true;
        }


    }
}