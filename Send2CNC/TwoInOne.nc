% 
O0101
(VERTICAL TUBE) 
 #531 = -2.215  (Workshift Value)
 #530 = 0.060   (Offset)
 G10 P0 Z#531   (set workshift)

(CCLNR-124 GENERAL TURN - KC 240)
 G0 T0909 M8
 G99 G96 S950 M3
 G0 X3.325 Z0.1 S950
 X3.3322
 Z-0.0691
 G3 X3.2474 Z-0.0866 R0.06 F0.012
 X3.1626 Z-0.0691 R0.06
 G1 X3.0434 Z-0.0095
 G2 X2.9974 Z0.0 R0.0325
 G1 X2.5126
 G3 X2.3926 Z0.06 R0.06
 G30 U0 W0
(1 1/2" BORING BAR)
 G0 T0505
 G99 G97 S1600
 G0 X3.0 Z0.1 S1600
 X2.569
 Z0.0605
 X2.7782
 G1 X2.8382 F0.012
 Z0.0121
 X2.715 Z-0.0496
 Z-0.968
 G0 X2.569
 Z0.0605
 M9
 G30 U0 W0
(CCLNR-124 GENERAL TURN - KC 240)
 G0 T0909
 G99 
 
 #529 = [#531+#530]  	(calculate workshift by offset amount)
 G10 P0 Z#529           (and set new workshift value)        
 M00

(CCLNR-124 GENERAL TURN - KC 240)
 G0 T0909 M8
 G99 G96 S950 M3
 G0 X3.325 Z0.1 S950
 X3.3322
 Z-0.0691
 G3 X3.2474 Z-0.0866 R0.06 F0.012
 X3.1626 Z-0.0691 R0.06
 G1 X3.0434 Z-0.0095
 G2 X2.9974 Z0.0 R0.0325
 G1 X2.5126
 G3 X2.3926 Z0.06 R0.06
 G30 U0 W0
(1 1/2" BORING BAR)
 G0 T0505
 G99 G97 S1600
 G0 X3.0 Z0.1 S1600
 X2.569
 Z0.0605
 X2.7782
 G1 X2.8382 F0.012
 Z0.0121
 X2.715 Z-0.0496
 Z-0.968
 G0 X2.569
 Z0.0605
 M9
 G30 U0 W0
(CCLNR-124 GENERAL TURN - KC 240)
 G0 T0909
 G99
 M30
