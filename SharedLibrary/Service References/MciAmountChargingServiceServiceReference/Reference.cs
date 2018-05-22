﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SharedLibrary.MciAmountChargingServiceServiceReference {
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
    public partial class PolicyException : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string messageIdField;
        
        private string textField;
        
        private string[] variablesField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string messageId {
            get {
                return this.messageIdField;
            }
            set {
                this.messageIdField = value;
                this.RaisePropertyChanged("messageId");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string text {
            get {
                return this.textField;
            }
            set {
                this.textField = value;
                this.RaisePropertyChanged("text");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("variables", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public string[] variables {
            get {
                return this.variablesField;
            }
            set {
                this.variablesField = value;
                this.RaisePropertyChanged("variables");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local")]
    public partial class chargeAmountResponse : object, System.ComponentModel.INotifyPropertyChanged {
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
    public partial class ChargingInformation : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string[] descriptionField;
        
        private string currencyField;
        
        private decimal amountField;
        
        private bool amountFieldSpecified;
        
        private string codeField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("description", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string[] description {
            get {
                return this.descriptionField;
            }
            set {
                this.descriptionField = value;
                this.RaisePropertyChanged("description");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string currency {
            get {
                return this.currencyField;
            }
            set {
                this.currencyField = value;
                this.RaisePropertyChanged("currency");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public decimal amount {
            get {
                return this.amountField;
            }
            set {
                this.amountField = value;
                this.RaisePropertyChanged("amount");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool amountSpecified {
            get {
                return this.amountFieldSpecified;
            }
            set {
                this.amountFieldSpecified = value;
                this.RaisePropertyChanged("amountSpecified");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public string code {
            get {
                return this.codeField;
            }
            set {
                this.codeField = value;
                this.RaisePropertyChanged("code");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local")]
    public partial class chargeAmount : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string endUserIdentifierField;
        
        private ChargingInformation chargeField;
        
        private string referenceCodeField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="anyURI", Order=0)]
        public string endUserIdentifier {
            get {
                return this.endUserIdentifierField;
            }
            set {
                this.endUserIdentifierField = value;
                this.RaisePropertyChanged("endUserIdentifier");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public ChargingInformation charge {
            get {
                return this.chargeField;
            }
            set {
                this.chargeField = value;
                this.RaisePropertyChanged("charge");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string referenceCode {
            get {
                return this.referenceCodeField;
            }
            set {
                this.referenceCodeField = value;
                this.RaisePropertyChanged("referenceCode");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
    public partial class ServiceException : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string messageIdField;
        
        private string textField;
        
        private string[] variablesField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string messageId {
            get {
                return this.messageIdField;
            }
            set {
                this.messageIdField = value;
                this.RaisePropertyChanged("messageId");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string text {
            get {
                return this.textField;
            }
            set {
                this.textField = value;
                this.RaisePropertyChanged("text");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("variables", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public string[] variables {
            get {
                return this.variablesField;
            }
            set {
                this.variablesField = value;
                this.RaisePropertyChanged("variables");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface", ConfigurationName="MciAmountChargingServiceServiceReference.AmountCharging")]
    public interface AmountCharging {
        
        // CODEGEN: Generating message contract since the operation chargeAmount is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/chargeAmountRequest", ReplyAction="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/chargeAmountResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(SharedLibrary.MciAmountChargingServiceServiceReference.PolicyException), Action="http://www.w3.org/2005/08/addressing/soap/fault", Name="PolicyException", Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
        [System.ServiceModel.FaultContractAttribute(typeof(SharedLibrary.MciAmountChargingServiceServiceReference.ServiceException), Action="http://www.w3.org/2005/08/addressing/soap/fault", Name="ServiceException", Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse1 chargeAmount(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/chargeAmountRequest", ReplyAction="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/chargeAmountResponse")]
        System.Threading.Tasks.Task<SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse1> chargeAmountAsync(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest request);
        
        // CODEGEN: Generating message contract since the operation refundAmount is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/refundAmountRequest", ReplyAction="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/refundAmountResponse")]
        [System.ServiceModel.FaultContractAttribute(typeof(SharedLibrary.MciAmountChargingServiceServiceReference.PolicyException), Action="http://www.w3.org/2005/08/addressing/soap/fault", Name="PolicyException", Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
        [System.ServiceModel.FaultContractAttribute(typeof(SharedLibrary.MciAmountChargingServiceServiceReference.ServiceException), Action="http://www.w3.org/2005/08/addressing/soap/fault", Name="ServiceException", Namespace="http://www.csapi.org/schema/parlayx/common/v4_0")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse1 refundAmount(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/refundAmountRequest", ReplyAction="http://www.csapi.org/wsdl/parlayx/payment/amount_charging/v4_0/interface/AmountCh" +
            "arging/refundAmountResponse")]
        System.Threading.Tasks.Task<SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse1> refundAmountAsync(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class chargeAmountRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local", Order=0)]
        public SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmount chargeAmount;
        
        public chargeAmountRequest() {
        }
        
        public chargeAmountRequest(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmount chargeAmount) {
            this.chargeAmount = chargeAmount;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class chargeAmountResponse1 {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local", Order=0)]
        public SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse chargeAmountResponse;
        
        public chargeAmountResponse1() {
        }
        
        public chargeAmountResponse1(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse chargeAmountResponse) {
            this.chargeAmountResponse = chargeAmountResponse;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local")]
    public partial class refundAmount : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string endUserIdentifierField;
        
        private ChargingInformation chargeField;
        
        private string referenceCodeField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType="anyURI", Order=0)]
        public string endUserIdentifier {
            get {
                return this.endUserIdentifierField;
            }
            set {
                this.endUserIdentifierField = value;
                this.RaisePropertyChanged("endUserIdentifier");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public ChargingInformation charge {
            get {
                return this.chargeField;
            }
            set {
                this.chargeField = value;
                this.RaisePropertyChanged("charge");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string referenceCode {
            get {
                return this.referenceCodeField;
            }
            set {
                this.referenceCodeField = value;
                this.RaisePropertyChanged("referenceCode");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.7.2612.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local")]
    public partial class refundAmountResponse : object, System.ComponentModel.INotifyPropertyChanged {
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class refundAmountRequest {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local", Order=0)]
        public SharedLibrary.MciAmountChargingServiceServiceReference.refundAmount refundAmount;
        
        public refundAmountRequest() {
        }
        
        public refundAmountRequest(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmount refundAmount) {
            this.refundAmount = refundAmount;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class refundAmountResponse1 {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.csapi.org/schema/parlayx/payment/amount_charging/v4_0/local", Order=0)]
        public SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse refundAmountResponse;
        
        public refundAmountResponse1() {
        }
        
        public refundAmountResponse1(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse refundAmountResponse) {
            this.refundAmountResponse = refundAmountResponse;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface AmountChargingChannel : SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class AmountChargingClient : System.ServiceModel.ClientBase<SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging>, SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging {
        
        public AmountChargingClient() {
        }
        
        public AmountChargingClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public AmountChargingClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public AmountChargingClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public AmountChargingClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse1 SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging.chargeAmount(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest request) {
            return base.Channel.chargeAmount(request);
        }
        
        public SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse chargeAmount(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmount chargeAmount1) {
            SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest inValue = new SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest();
            inValue.chargeAmount = chargeAmount1;
            SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse1 retVal = ((SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging)(this)).chargeAmount(inValue);
            return retVal.chargeAmountResponse;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse1> SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging.chargeAmountAsync(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest request) {
            return base.Channel.chargeAmountAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountResponse1> chargeAmountAsync(SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmount chargeAmount) {
            SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest inValue = new SharedLibrary.MciAmountChargingServiceServiceReference.chargeAmountRequest();
            inValue.chargeAmount = chargeAmount;
            return ((SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging)(this)).chargeAmountAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse1 SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging.refundAmount(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest request) {
            return base.Channel.refundAmount(request);
        }
        
        public SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse refundAmount(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmount refundAmount1) {
            SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest inValue = new SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest();
            inValue.refundAmount = refundAmount1;
            SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse1 retVal = ((SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging)(this)).refundAmount(inValue);
            return retVal.refundAmountResponse;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse1> SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging.refundAmountAsync(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest request) {
            return base.Channel.refundAmountAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountResponse1> refundAmountAsync(SharedLibrary.MciAmountChargingServiceServiceReference.refundAmount refundAmount) {
            SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest inValue = new SharedLibrary.MciAmountChargingServiceServiceReference.refundAmountRequest();
            inValue.refundAmount = refundAmount;
            return ((SharedLibrary.MciAmountChargingServiceServiceReference.AmountCharging)(this)).refundAmountAsync(inValue);
        }
    }
}