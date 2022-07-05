import java.nio.charset.StandardCharsets;
import java.io.FileReader;
import java.io.BufferedReader;


public class RainbowStripper {
	public static void main(String[] args) throws Exception {

		try (BufferedReader br = new BufferedReader(new FileReader(args[0]))) {
			String line;
			while ((line = br.readLine()) != null) {
				if (line.length() > 9) {
					System.out.print(line.substring(9) + "\n");
				}
			}
		}
	}
}
