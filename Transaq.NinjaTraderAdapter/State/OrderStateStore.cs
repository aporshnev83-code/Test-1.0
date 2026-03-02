using System;
using System.Collections.Generic;

namespace Transaq.NinjaTraderAdapter.State;

public sealed class OrderStateStore
{
    private readonly Dictionary<long, OrderState> _byTxId = new();
    private readonly Dictionary<long, long> _txByOrderNo = new();

    public OrderState Upsert(long transactionId, long orderNo, string transaqStatus, decimal quantity, decimal filled)
    {
        if (!_byTxId.TryGetValue(transactionId, out var state))
        {
            state = new OrderState { TransactionId = transactionId };
            _byTxId[transactionId] = state;
        }

        if (state.OrderNo == 0 && orderNo > 0)
        {
            state.OrderNo = orderNo;
            _txByOrderNo[orderNo] = transactionId;
        }
        else if (orderNo > 0)
        {
            state.OrderNo = orderNo;
            _txByOrderNo[orderNo] = transactionId;
        }

        state.Status = transaqStatus;
        state.NtState = MapState(transaqStatus, quantity, filled);
        state.Quantity = quantity;
        state.Filled = filled;
        state.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return state;
    }

    public bool TryGetByTx(long txId, out OrderState? state) => _byTxId.TryGetValue(txId, out state);

    public bool TryGetByOrderNo(long orderNo, out OrderState? state)
    {
        if (_txByOrderNo.TryGetValue(orderNo, out var tx))
        {
            return _byTxId.TryGetValue(tx, out state);
        }

        state = null;
        return false;
    }

    private static NtOrderState MapState(string transaq, decimal quantity, decimal filled) => transaq switch
    {
        "active" => NtOrderState.Working,
        "cancelled" => NtOrderState.Cancelled,
        "rejected" => NtOrderState.Rejected,
        "matched" when filled >= quantity && quantity > 0 => NtOrderState.Filled,
        "matched" when filled > 0 => NtOrderState.PartFilled,
        "matched" => NtOrderState.Working,
        _ => NtOrderState.Unknown
    };
}
