<html>
<head>
   <style type="text/css">
      body { font-family: Arial, Helvetica, sans-serif; font-size:13px; color:#222426; background:#ffffff; }
      table { padding:5px; }
      td { padding:5px; }
   </style>
</head>
<body>
<?php 
   //echo date('Y-m-d H:i:s'); 
   $conn = new mysqli("localhost","root","raspberry","IrrigationController");
   if ($conn->connect_error) {
      die("Connection failed " . $conn->connect_error);
   }

   $sqlStatus = "SELECT Mode, State, TStamp, Pressure from ControllerStatus";
   $resultStatus = $conn->query($sqlStatus);
   $rowStatus = $resultStatus->fetch_assoc();

   $sqlFault = "SELECT TStamp, Description from EventHistory where EventType = 2 order by TStamp desc LIMIT 1";
   $resultFault = $conn->query($sqlFault);
   $rowFault = $resultFault->fetch_assoc();

   $sqlBoot = "SELECT TStamp from EventHistory where Description like '%started%' order by TStamp desc LIMIT 1";
   $resultBoot = $conn->query($sqlBoot);
   $rowBoot = $resultBoot->fetch_assoc();
?>

   <h1>Irrigation Controller Status</h1>
      <table>
         <tr>
            <td>
               Mode:
            </td>
            <td>
               <?php echo $rowStatus["Mode"]; ?>
            </td>
         </tr>
         <tr>
            <td>
               State:
            </td>
            <td>
               <?php echo $rowStatus["State"]; ?>
            </td>
         </tr>
         <tr>
            <td>
               Last fault event:
            </td>
            <td>
               <?php echo $rowFault["Description"] ." - ". $rowFault["TStamp"]; ?>
            </td>
         </tr>

         <tr>
            <td>
               Last update:
            </td>
            <td>
               <?php echo $rowStatus["TStamp"]; ?>
            </td>
         </tr>

         <tr>
            <td>
               Last reboot:
            </td>
            <td>
               <?php echo $rowBoot["TStamp"]; ?>
            </td>
         </tr>
	<tr>
		<td>
			Pressure:
		</td>
		<td>
			<?php echo $rowStatus["Pressure"]; ?>
		</td>
	</tr>
      </table>
<?php $conn->close(); ?>
</body>
</html>
