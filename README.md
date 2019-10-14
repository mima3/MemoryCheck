# MemoryCheck
.NETのプロセスにおいて、COMの解放漏れを検出します。

詳細は下記を参照してください。

**.NETを使ったOfficeの自動化が面倒なはずがない―そう考えていた時期が俺にもありました。**
https://qiita.com/mima_ita/items/aa811423d8c4410eca71

使用例

```
>EnumRcw powershell
powershell
powershell 22112 =======================================================
X86
         4A5534C           16 System.__ComObject 1 False Windows.Foundation.Diagnostics.IAsyncCausalityTracerStatics,
         4A5537C           16 Windows.Foundation.Diagnostics.TracingStatusChangedEventArgs 1 False Windows.Foundation.Diagnostics.ITracingStatusChangedEventArgs,

>WalkHeap powershell 4A5537C
powershell 22112 =======================================================
-----
find
roots...
          4A5537C - System.Threading.ExecutionContext - 44 bytes
+          4A543D4 - System.Threading.Thread - 52 bytes
Finalize
```
