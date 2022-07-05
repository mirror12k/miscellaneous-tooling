import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.io.FileReader;
import java.io.BufferedReader;


public class RainbowSorter {
	private static final char[] hexArray = "0123456789abcdef".toCharArray();

	private static String bytesToHex(byte[] bytes) {
		char[] hexChars = new char[bytes.length * 2];
		for ( int j = 0; j < bytes.length; j++ ) {
			int v = bytes[j] & 0xFF;
			hexChars[j * 2] = hexArray[v >>> 4];
			hexChars[j * 2 + 1] = hexArray[v & 0x0F];
		}
		return String.valueOf(hexChars);
	}

	private static String bytesToHex(byte[] bytes, int limit) {
		char[] hexChars = new char[limit * 2];
		for ( int j = 0; j < limit; j++ ) {
			int v = bytes[j] & 0xFF;
			hexChars[j * 2] = hexArray[v >>> 4];
			hexChars[j * 2 + 1] = hexArray[v & 0x0F];
		}
		return String.valueOf(hexChars);
	}

	public static void main(String[] args) throws Exception {
		// System.out.println("hello world!\n");
		MessageDigest digest = MessageDigest.getInstance("SHA-256");

		try (BufferedReader br = new BufferedReader(new FileReader(args[0]))) {
			String line;
			while ((line = br.readLine()) != null) {
				System.out.print(bytesToHex(digest.digest(line.getBytes(StandardCharsets.UTF_8)), 4) + ":" + line + "\n");
			}
		}
	}
}
