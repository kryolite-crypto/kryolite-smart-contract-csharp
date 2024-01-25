using System.Runtime.InteropServices;

namespace Kryolite.SmartContract
{
    public static class API
    {
        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__exit")]
        public static unsafe extern void __exit(int exitCode);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__rand")]
        public static unsafe extern float __rand();

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__transfer")]
        public static unsafe extern void __transfer(byte* addrPtr, ulong value);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__transfer_token")]
        public static unsafe extern void __transfer_token(byte* from_addr, byte* to_addr, byte* token_id);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__consume_token")]
        public static unsafe extern void __consume_token(byte* owner_addr, byte* token_id);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__approval")]
        public static unsafe extern void __approval(byte* from_addr, byte* to_addr, byte* token_id);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__println")]
        public static unsafe extern void __println(byte* val_ptr, int val_len);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__append_event")]
        public static unsafe extern void __append_event(byte* val_ptr, int val_len);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__publish_event")]
        public static unsafe extern void __publish_event();

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__return")]
        public static unsafe extern void __return(byte* buf, int size);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__hash_data")]
        public static unsafe extern void __hash_data(byte* buf, int size, byte* dest, int dest_len);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__schedule_param")]
        public static unsafe extern void __schedule_param(byte* buf, int len);

        [WasmImportLinkage]
        [DllImport("env", EntryPoint = "__schedule")]
        public static unsafe extern void __schedule(byte* buf, int len, long timestamp);
    }
}

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class WasmImportLinkageAttribute : Attribute
    {
        public WasmImportLinkageAttribute() {}
    }
}
