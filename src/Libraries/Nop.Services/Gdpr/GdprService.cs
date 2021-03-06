using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Gdpr;
using Nop.Services.Authentication.External;
using Nop.Services.Blogs;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Forums;
using Nop.Services.Messages;
using Nop.Services.News;
using Nop.Services.Orders;
using Nop.Services.Stores;

namespace Nop.Services.Gdpr
{
    /// <summary>
    /// Represents the GDPR service
    /// </summary>
    public partial class GdprService : IGdprService
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly IBackInStockSubscriptionService _backInStockSubscriptionService;
        private readonly IBlogService _blogService;
        private readonly ICustomerService _customerService;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IForumService _forumService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
        private readonly INewsService _newsService;
        private readonly IProductService _productService;
        private readonly IRepository<GdprConsent> _gdprConsentRepository;
        private readonly IRepository<GdprLog> _gdprLogRepository;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreService _storeService;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="addressService">Address service</param>
        /// <param name="backInStockSubscriptionService">Back in stock subscription service</param>
        /// <param name="blogService">Blog service</param>
        /// <param name="customerService">Customer service</param>
        /// <param name="externalAuthenticationService">External authentication service</param>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="forumService">Forum service</param>
        /// <param name="genericAttributeService">Generic attribute service</param>
        /// <param name="newsService">News service</param>
        /// <param name="newsLetterSubscriptionService">NewsLetter subscription service</param>
        /// <param name="productService">Product service</param>
        /// <param name="gdprConsentRepository">GDPR consent repository</param>
        /// <param name="gdprLogRepository">GDPR log repository</param>
        /// <param name="shoppingCartService">Shopping cart service</param>
        /// <param name="storeService">Store service</param>
        public GdprService(IAddressService addressService,
            IBackInStockSubscriptionService backInStockSubscriptionService,
            IBlogService blogService,
            ICustomerService customerService,
            IExternalAuthenticationService externalAuthenticationService,
            IEventPublisher eventPublisher,
            IForumService forumService,
            IGenericAttributeService genericAttributeService,
            INewsService newsService,
            INewsLetterSubscriptionService newsLetterSubscriptionService,
            IProductService productService,
            IRepository<GdprConsent> gdprConsentRepository,
            IRepository<GdprLog> gdprLogRepository,
            IShoppingCartService shoppingCartService,
            IStoreService storeService)
        {
            this._addressService = addressService;
            this._backInStockSubscriptionService = backInStockSubscriptionService;
            this._blogService = blogService;
            this._customerService = customerService;
            this._externalAuthenticationService = externalAuthenticationService;
            this._eventPublisher = eventPublisher;
            this._forumService = forumService;
            this._genericAttributeService = genericAttributeService;
            this._newsService = newsService;
            this._newsLetterSubscriptionService = newsLetterSubscriptionService;
            this._productService = productService;
            this._gdprConsentRepository = gdprConsentRepository;
            this._gdprLogRepository = gdprLogRepository;
            this._shoppingCartService = shoppingCartService;
            this._storeService = storeService;
        }

        #endregion

        #region Methods

        #region GDPR consent

        /// <summary>
        /// Get a GDPR consent
        /// </summary>
        /// <param name="gdprConsentId">The GDPR consent identifier</param>
        /// <returns>GDPR consent</returns>
        public virtual GdprConsent GetConsentById(int gdprConsentId)
        {
            if (gdprConsentId == 0)
                return null;

            return _gdprConsentRepository.GetById(gdprConsentId);
        }

        /// <summary>
        /// Get all GDPR consents
        /// </summary>
        /// <returns>GDPR consent</returns>
        public virtual IList<GdprConsent> GetAllConsents()
        {
            var query = from c in _gdprConsentRepository.Table
                        orderby c.DisplayOrder, c.Id
                        select c;
            var gdprConsents = query.ToList();
            return gdprConsents;
        }

        /// <summary>
        /// Insert a GDPR consent
        /// </summary>
        /// <param name="gdprConsent">GDPR consent</param>
        public virtual void InsertConsent(GdprConsent gdprConsent)
        {
            if (gdprConsent == null)
                throw new ArgumentNullException(nameof(gdprConsent));

            _gdprConsentRepository.Insert(gdprConsent);

            //event notification
            _eventPublisher.EntityInserted(gdprConsent);
        }

        /// <summary>
        /// Update the GDPR consent
        /// </summary>
        /// <param name="gdprConsent">GDPR consent</param>
        public virtual void UpdateConsent(GdprConsent gdprConsent)
        {
            if (gdprConsent == null)
                throw new ArgumentNullException(nameof(gdprConsent));

            _gdprConsentRepository.Update(gdprConsent);

            //event notification
            _eventPublisher.EntityUpdated(gdprConsent);
        }

        /// <summary>
        /// Delete a GDPR consent
        /// </summary>
        /// <param name="gdprConsent">GDPR consent</param>
        public virtual void DeleteConsent(GdprConsent gdprConsent)
        {
            if (gdprConsent == null)
                throw new ArgumentNullException(nameof(gdprConsent));

            _gdprConsentRepository.Delete(gdprConsent);

            //event notification
            _eventPublisher.EntityDeleted(gdprConsent);
        }

        /// <summary>
        /// Gets the latest selected value (a consent is accepted or not by a customer)
        /// </summary>
        /// <param name="consentId">Consent identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <returns>Result; null if previous a customer hasn't been asked</returns>
        public virtual bool? IsConsentAccepted(int consentId, int customerId)
        {
            //get latest record
            var log = GetAllLog(customerId: customerId, consentId: consentId, pageIndex: 0, pageSize: 1).FirstOrDefault();
            if (log == null)
                return null;

            switch (log.RequestType)
            {
                case GdprRequestType.ConsentAgree:
                    return true;
                case GdprRequestType.ConsentDisagree:
                    return false;
                default:
                    return null;
            }
        }
        #endregion

        #region GDPR log

        /// <summary>
        /// Get a GDPR log
        /// </summary>
        /// <param name="gdprLogId">The GDPR log identifier</param>
        /// <returns>GDPR log</returns>
        public virtual GdprLog GetLogById(int gdprLogId)
        {
            if (gdprLogId == 0)
                return null;

            return _gdprLogRepository.GetById(gdprLogId);
        }

        /// <summary>
        /// Get all GDPR log records
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="consentId">Consent identifier</param>
        /// <param name="customerInfo">Customer info (Exact match)</param>
        /// <param name="requestType">GDPR request type</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>GDPR log records</returns>
        public virtual IPagedList<GdprLog> GetAllLog(int customerId = 0, int consentId = 0,
            string customerInfo = "", GdprRequestType? requestType = null,
            int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _gdprLogRepository.Table;
            if (customerId > 0)
            {
                query = query.Where(log => log.CustomerId == customerId);
            }
            if (consentId > 0)
            {
                query = query.Where(log => log.ConsentId == consentId);
            }
            if (!String.IsNullOrEmpty(customerInfo))
            {
                query = query.Where(log => log.CustomerInfo == customerInfo);
            }
            if (requestType != null)
            {
                int requestTypeId = (int)requestType;
                query = query.Where(log => log.RequestTypeId == requestTypeId);
            }

            query = query.OrderByDescending(log => log.CreatedOnUtc).ThenByDescending(log => log.Id);
            return new PagedList<GdprLog>(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Insert a GDPR log
        /// </summary>
        /// <param name="gdprLog">GDPR log</param>
        public virtual void InsertLog(GdprLog gdprLog)
        {
            if (gdprLog == null)
                throw new ArgumentNullException(nameof(gdprLog));

            _gdprLogRepository.Insert(gdprLog);

            //event notification
            _eventPublisher.EntityInserted(gdprLog);
        }

        /// <summary>
        /// Insert a GDPR log
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="consentId">Consent identifier</param>
        /// <param name="requestType">Request type</param>
        /// <param name="requestDetails">Request details</param>
        public virtual void InsertLog(Customer customer, int consentId, GdprRequestType requestType, string requestDetails)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var gdprLog = new GdprLog
            {
                CustomerId = customer.Id,
                ConsentId = consentId,
                CustomerInfo = customer.Email,
                RequestType = requestType,
                RequestDetails = requestDetails,
                CreatedOnUtc = DateTime.UtcNow
            };
            InsertLog(gdprLog);
        }

        /// <summary>
        /// Update the GDPR log
        /// </summary>
        /// <param name="gdprLog">GDPR log</param>
        public virtual void UpdateLog(GdprLog gdprLog)
        {
            if (gdprLog == null)
                throw new ArgumentNullException(nameof(gdprLog));

            _gdprLogRepository.Update(gdprLog);

            //event notification
            _eventPublisher.EntityUpdated(gdprLog);
        }

        /// <summary>
        /// Delete a GDPR log
        /// </summary>
        /// <param name="gdprLog">GDPR log</param>
        public virtual void DeleteLog(GdprLog gdprLog)
        {
            if (gdprLog == null)
                throw new ArgumentNullException(nameof(gdprLog));

            _gdprLogRepository.Delete(gdprLog);

            //event notification
            _eventPublisher.EntityDeleted(gdprLog);
        }

        #endregion

        #region Customer

        /// <summary>
        /// Permanent delete of customer
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual void PermanentDeleteCustomer(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            //blog comments
            var blogComments = _blogService.GetAllComments(customerId: customer.Id);
            _blogService.DeleteBlogComments(blogComments);

            //news comments
            var newsComments = _newsService.GetAllComments(customerId: customer.Id);
            _newsService.DeleteNewsComments(newsComments);

            //back in stock subscriptions
            var backInStockSubscriptions = _backInStockSubscriptionService.GetAllSubscriptionsByCustomerId(customer.Id);
            foreach (var backInStockSubscription in backInStockSubscriptions)
                _backInStockSubscriptionService.DeleteSubscription(backInStockSubscription);

            //product review
            var productReviews = _productService.GetAllProductReviews(customerId: customer.Id, approved: null);
            var reviewedProducts = _productService.GetProductsByIds(productReviews.Select(p => p.ProductId).Distinct().ToArray());
            _productService.DeleteProductReviews(productReviews);
            //update product totals
            foreach (var product in reviewedProducts)
            {
                _productService.UpdateProductReviewTotals(product);
            }

            //external authentication record
            foreach (var ear in customer.ExternalAuthenticationRecords)
                _externalAuthenticationService.DeleteExternalAuthenticationRecord(ear);
            
            //forum subscriptions
            var forumSubscriptions = _forumService.GetAllSubscriptions(customerId: customer.Id);
            foreach (var forumSubscription in forumSubscriptions)
                _forumService.DeleteSubscription(forumSubscription);

            //shopping cart items
            foreach (var sci in customer.ShoppingCartItems)
                _shoppingCartService.DeleteShoppingCartItem(sci);
             
            //private messages (sent)
            foreach (var pm in _forumService.GetAllPrivateMessages(storeId: 0, fromCustomerId: customer.Id, toCustomerId: 0,
                isRead: null, isDeletedByAuthor: null, isDeletedByRecipient: null, keywords: null))
                _forumService.DeletePrivateMessage(pm);
            //private messages (received)
            foreach (var pm in _forumService.GetAllPrivateMessages(storeId: 0, fromCustomerId: 0, toCustomerId: customer.Id,
                isRead: null, isDeletedByAuthor: null, isDeletedByRecipient: null, keywords: null))
                _forumService.DeletePrivateMessage(pm);

            //newsletter
            var allStores = _storeService.GetAllStores();
            foreach (var store in allStores)
            {
                var newsletter = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(customer.Email, store.Id);
                if (newsletter != null)
                    _newsLetterSubscriptionService.DeleteNewsLetterSubscription(newsletter);
            }

            //addresses
            foreach (var address in customer.Addresses)
            {
                customer.RemoveAddress(address);
                _customerService.UpdateCustomer(customer);
                //now delete the address record
                _addressService.DeleteAddress(address);
            }

            //generic attributes
            var keyGroup = customer.GetType().BaseType.Name;
            var genericAttributes = _genericAttributeService.GetAttributesForEntity(customer.Id, keyGroup);
            _genericAttributeService.DeleteAttributes(genericAttributes);

            //ignore ActivityLog
            //ignore ForumPost, ForumTopic, ignore ForumPostVote
            //ignore Log
            //ignore PollVotingRecord
            //ignore ProductReviewHelpfulness
            //ignore RecurringPayment 
            //ignore ReturnRequest
            //ignore RewardPointsHistory
            //and we do not delete orders

            //remove from Registered role, add to Guest one
            if (customer.IsRegistered())
            {
                var registeredRole = _customerService.GetCustomerRoleBySystemName(SystemCustomerRoleNames.Registered);
                customer.CustomerCustomerRoleMappings
                    .Remove(customer.CustomerCustomerRoleMappings.FirstOrDefault(mapping => mapping.CustomerRoleId == registeredRole.Id));
            }
            if (!customer.IsGuest())
            {
                var guestRole = _customerService.GetCustomerRoleBySystemName(SystemCustomerRoleNames.Guests);
                customer.CustomerCustomerRoleMappings.Add(new CustomerCustomerRoleMapping { CustomerRole = guestRole });
            }

            var email = customer.Email;

            //clear other information
            customer.Email = "";
            customer.EmailToRevalidate = "";
            customer.Username = "";
            customer.Active = false;
            customer.Deleted = true;
            _customerService.UpdateCustomer(customer);

            //raise event
            _eventPublisher.Publish(new CustomerPermanentlyDeleted(customer.Id, email));
        }

        #endregion

        #endregion
    }
}
