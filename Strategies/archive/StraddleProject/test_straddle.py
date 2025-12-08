#!/usr/bin/env python3
"""
Test script for StraddleGexFlow algorithm
"""

from StraddleGexFlow import StraddleGexFlow
from mock_quantconnect import OptionRight

def test_algorithm():
    """Test the StraddleGexFlow algorithm initialization and basic operations"""
    
    print("Testing StraddleGexFlow Algorithm...")
    print("=" * 50)
    
    try:
        # Create algorithm instance
        algorithm = StraddleGexFlow()
        print("✓ Algorithm instance created successfully")
        
        # Initialize the algorithm
        algorithm.Initialize()
        print("✓ Algorithm initialized successfully")
        
        # Test volume history update
        algorithm.UpdateVolumeHistory()
        print("✓ Volume history updated successfully")
        
        # Test signal checking
        algorithm.CheckSignals()
        print("✓ Signal checking completed successfully")
        
        # Test helper functions
        for symbol in algorithm.symbols:
            print(f"\nTesting functions for {symbol}:")
            
            # Test expiry picking
            expiry = algorithm.PickExpiry(symbol, 30, 50)
            print(f"  PickExpiry: {expiry}")
            
            # Test ATM option picking
            spot = algorithm.Securities[algorithm.equities[symbol]].Price
            call = algorithm.PickAtm(symbol, OptionRight.Call, expiry, spot)
            put = algorithm.PickAtm(symbol, OptionRight.Put, expiry, spot)
            print(f"  ATM Call: {call}")
            print(f"  ATM Put: {put}")
        
        # Test ORATS data fetching (will likely fail but shouldn't crash)
        print(f"\nTesting ORATS data fetch:")
        orats_data = algorithm.GetOratsData("NVDA", 100)
        print(f"  ORATS data: {orats_data}")
        
        print("\n" + "=" * 50)
        print("✅ ALL TESTS PASSED - Algorithm runs without errors!")
        return True
        
    except Exception as e:
        print(f"\n❌ ERROR: {str(e)}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    success = test_algorithm()
    exit(0 if success else 1)